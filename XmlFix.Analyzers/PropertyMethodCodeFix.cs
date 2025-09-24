using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers;

/// <summary>
/// Code fix provider for converting methods to properties.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyMethodCodeFix)), Shared]
public sealed class PropertyMethodCodeFix : CodeFixProvider
{
    /// <summary>
    /// Gets the diagnostic IDs that this code fix provider can fix.
    /// </summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(AnalyzerConstants.UsePropertiesWhereAppropriateId);

    /// <summary>
    /// Gets the fix all provider for batch fixing.
    /// </summary>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers code fixes for the specified diagnostics.
    /// </summary>
    /// <param name="context">The code fix context.</param>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        Diagnostic? diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == AnalyzerConstants.UsePropertiesWhereAppropriateId);
        if (diagnostic == null) return;

        SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan);
        if (node == null) return;

        MethodDeclarationSyntax? methodDeclaration = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration == null) return;

        // Register the code action for converting to property
        var convertToPropertyAction = CodeAction.Create(
            title: "Convert to property",
            createChangedDocument: c => ConvertMethodToPropertyAsync(context.Document, methodDeclaration, c),
            equivalenceKey: "ConvertToProperty");

        context.RegisterCodeFix(convertToPropertyAction, diagnostic);

        // If the method starts with "Get", also offer to create an auto-property
        if (methodDeclaration.Identifier.ValueText.StartsWith("Get", System.StringComparison.Ordinal) &&
            IsSimpleReturnMethod(methodDeclaration))
        {
            var createAutoPropertyAction = CodeAction.Create(
                title: "Convert to auto-property",
                createChangedDocument: c => ConvertToAutoPropertyAsync(context.Document, methodDeclaration, c),
                equivalenceKey: "ConvertToAutoProperty");

            context.RegisterCodeFix(createAutoPropertyAction, diagnostic);
        }
    }

    /// <summary>
    /// Converts a method declaration to a property declaration.
    /// </summary>
    /// <param name="document">The document containing the method.</param>
    /// <param name="methodDeclaration">The method to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document.</returns>
    private static async Task<Document> ConvertMethodToPropertyAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        string propertyName = GetPropertyName(methodDeclaration.Identifier.ValueText);
        PropertyDeclarationSyntax property = CreatePropertyFromMethod(methodDeclaration, propertyName);

        // Apply formatting that matches the document's style
        var formattedProperty = property.WithAdditionalAnnotations(Formatter.Annotation);
        SyntaxNode newRoot = root.ReplaceNode(methodDeclaration, formattedProperty);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Converts a method declaration to an auto-property with backing field.
    /// </summary>
    /// <param name="document">The document containing the method.</param>
    /// <param name="methodDeclaration">The method to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document.</returns>
    private static async Task<Document> ConvertToAutoPropertyAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        string propertyName = GetPropertyName(methodDeclaration.Identifier.ValueText);
        PropertyDeclarationSyntax autoProperty = CreateAutoPropertyFromMethod(methodDeclaration, propertyName);

        // Apply formatting that matches the document's style
        var formattedAutoProperty = autoProperty.WithAdditionalAnnotations(Formatter.Annotation);
        SyntaxNode newRoot = root.ReplaceNode(methodDeclaration, formattedAutoProperty);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Creates a property declaration from a method declaration.
    /// </summary>
    /// <param name="method">The method to convert.</param>
    /// <param name="propertyName">The name for the property.</param>
    /// <returns>The property declaration.</returns>
    private static PropertyDeclarationSyntax CreatePropertyFromMethod(
        MethodDeclarationSyntax method,
        string propertyName)
    {
        // For expression-bodied methods, create expression-bodied properties
        if (method.ExpressionBody != null)
        {
            return SyntaxFactory.PropertyDeclaration(method.ReturnType, propertyName)
                .WithModifiers(method.Modifiers)
                .WithExpressionBody(method.ExpressionBody)
                .WithSemicolonToken(method.SemicolonToken)
                .WithLeadingTrivia(method.GetLeadingTrivia())
                .WithTrailingTrivia(method.GetTrailingTrivia());
        }

        // For block-bodied methods, create properties with getter accessors
        BlockSyntax? methodBody = method.Body;
        if (methodBody == null)
        {
            // Fallback for methods without body
            AccessorDeclarationSyntax getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            AccessorListSyntax accessorList = SyntaxFactory.AccessorList(
                SyntaxFactory.SingletonList(getter));

            return SyntaxFactory.PropertyDeclaration(method.ReturnType, propertyName)
                .WithModifiers(method.Modifiers)
                .WithAccessorList(accessorList)
                .WithLeadingTrivia(method.GetLeadingTrivia())
                .WithTrailingTrivia(method.GetTrailingTrivia());
        }

        // Create getter with block body
        AccessorDeclarationSyntax blockGetter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithBody(methodBody);

        AccessorListSyntax blockAccessorList = SyntaxFactory.AccessorList(
            SyntaxFactory.SingletonList(blockGetter));

        return SyntaxFactory.PropertyDeclaration(method.ReturnType, propertyName)
            .WithModifiers(method.Modifiers)
            .WithAccessorList(blockAccessorList)
            .WithLeadingTrivia(method.GetLeadingTrivia())
            .WithTrailingTrivia(method.GetTrailingTrivia());
    }

    /// <summary>
    /// Creates an auto-property declaration from a method declaration.
    /// </summary>
    /// <param name="method">The method to convert.</param>
    /// <param name="propertyName">The name for the property.</param>
    /// <returns>The auto-property declaration.</returns>
    private static PropertyDeclarationSyntax CreateAutoPropertyFromMethod(
        MethodDeclarationSyntax method,
        string propertyName)
    {
        // Create simple auto-property accessors
        AccessorDeclarationSyntax getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        AccessorDeclarationSyntax setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        AccessorListSyntax accessorList = SyntaxFactory.AccessorList(
            SyntaxFactory.List(new[] { getter, setter }));

        // Create the auto-property with minimal trivia to avoid spacing issues
        PropertyDeclarationSyntax property = SyntaxFactory.PropertyDeclaration(method.ReturnType, propertyName)
            .WithModifiers(method.Modifiers)
            .WithAccessorList(accessorList)
            .WithAdditionalAnnotations(Formatter.Annotation);

        return property;
    }

    /// <summary>
    /// Cleans up leading trivia by removing blank lines for auto-properties.
    /// </summary>
    /// <param name="trivia">The original trivia.</param>
    /// <returns>Cleaned trivia with no leading blank lines.</returns>
    private static SyntaxTriviaList CleanLeadingTrivia(SyntaxTriviaList trivia)
    {
        // For auto-properties, remove all leading blank lines to avoid extra spacing
        var cleanedTrivia = new List<SyntaxTrivia>();

        foreach (var trivium in trivia)
        {
            // Skip EndOfLine trivia (blank lines) but keep other trivia like comments
            if (!trivium.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                cleanedTrivia.Add(trivium);
            }
        }

        return SyntaxFactory.TriviaList(cleanedTrivia);
    }

    /// <summary>
    /// Creates a getter accessor from a method declaration.
    /// </summary>
    /// <param name="method">The method to convert.</param>
    /// <returns>The getter accessor declaration.</returns>
    private static AccessorDeclarationSyntax CreateGetterFromMethod(MethodDeclarationSyntax method)
    {
        AccessorDeclarationSyntax getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);

        // Handle expression-bodied methods
        if (method.ExpressionBody != null)
        {
            getter = getter.WithExpressionBody(method.ExpressionBody)
                          .WithSemicolonToken(method.SemicolonToken);
        }
        // Handle block-bodied methods
        else if (method.Body != null)
        {
            getter = getter.WithBody(method.Body);
        }
        // Fallback for abstract or incomplete methods
        else
        {
            getter = getter.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        return getter;
    }

    /// <summary>
    /// Derives a property name from a method name.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <returns>The property name.</returns>
    private static string GetPropertyName(string methodName)
    {
        // Remove common prefixes
        foreach (string prefix in AnalyzerConstants.CommonPropertyMethodPrefixes)
        {
            if (methodName.StartsWith(prefix, StringComparison.Ordinal) &&
                methodName.Length > prefix.Length)
            {
                string remainder = methodName.Substring(prefix.Length);

                // Ensure the remainder starts with an uppercase letter
                if (remainder.Length > 0 && char.IsUpper(remainder[0]))
                {
                    return remainder;
                }
            }
        }

        // If no prefix matches, use the original name
        return methodName;
    }

    /// <summary>
    /// Determines if a method is a simple return method suitable for auto-property conversion.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True if the method is a simple return method.</returns>
    private static bool IsSimpleReturnMethod(MethodDeclarationSyntax method)
    {
        // Check for expression-bodied methods that return a field
        if (method.ExpressionBody?.Expression is IdentifierNameSyntax)
            return true;

        // Check for simple block methods that return a field
        if (method.Body?.Statements.Count == 1 &&
            method.Body.Statements[0] is ReturnStatementSyntax returnStatement &&
            returnStatement.Expression is IdentifierNameSyntax)
            return true;

        return false;
    }
}