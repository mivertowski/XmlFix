using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers;

/// <summary>
/// Code fix provider for adding StringComparison parameters to string comparison methods.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringComparisonCodeFix)), Shared]
public sealed class StringComparisonCodeFix : CodeFixProvider
{
    /// <summary>
    /// Gets the diagnostic IDs that this code fix provider can fix.
    /// </summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(AnalyzerConstants.SpecifyStringComparisonId);

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
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == AnalyzerConstants.SpecifyStringComparisonId);
        if (diagnostic == null) return;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        if (node == null) return;

        var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation?.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.ValueText;

        // Register common StringComparison options based on method context
        var commonSuggestions = GetContextBasedSuggestions(methodName, invocation);

        foreach (var stringComparison in commonSuggestions)
        {
            var comparisonName = stringComparison.Split('.').Last(); // Get just the enum value name
            var title = $"Use {comparisonName}";

            var codeAction = CodeAction.Create(
                title: title,
                createChangedDocument: c => AddStringComparisonAsync(context.Document, invocation, stringComparison, c),
                equivalenceKey: $"AddStringComparison_{comparisonName}");

            context.RegisterCodeFix(codeAction, diagnostic);
        }
    }

    /// <summary>
    /// Adds a StringComparison argument to a string method invocation.
    /// </summary>
    /// <param name="document">The document containing the invocation.</param>
    /// <param name="invocation">The invocation to modify.</param>
    /// <param name="stringComparison">The StringComparison value to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document.</returns>
    private static async Task<Document> AddStringComparisonAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        string stringComparison,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Create the StringComparison argument using the provided comparison value
        var comparisonValue = stringComparison.Split('.').Last(); // Extract the enum value (e.g., "Ordinal" from "StringComparison.Ordinal")
        var comparisonArgument = SyntaxFactory.Argument(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("StringComparison"),
                SyntaxFactory.IdentifierName(comparisonValue)));

        // Add the argument to the invocation
        var newInvocation = invocation.AddArgumentListArguments(comparisonArgument);

        // Replace the invocation in the tree first
        var newRoot = root.ReplaceNode(invocation, newInvocation);

        // Add using directive for System namespace if needed
        newRoot = await EnsureSystemUsingDirectiveAsync(document, newRoot, cancellationToken).ConfigureAwait(false);

        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Ensures that the System using directive is present in the compilation unit.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="root">The syntax root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated syntax root.</returns>
    private static Task<SyntaxNode> EnsureSystemUsingDirectiveAsync(
        Document document,
        SyntaxNode root,
        CancellationToken cancellationToken)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
            return Task.FromResult(root);

        if (!HasUsingDirective(compilationUnit, "System"))
        {
            // Create using directive with only a single newline
            var systemUsing = SyntaxFactory.UsingDirective(
                SyntaxFactory.IdentifierName("System"))
                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

            // Add the using directive
            var newUsings = compilationUnit.Usings.Add(systemUsing);

            // Preserve the original compilation unit's leading trivia (including any initial blank line)
            var originalLeadingTrivia = compilationUnit.GetLeadingTrivia();

            var newCompilationUnit = compilationUnit
                .WithUsings(newUsings)
                .WithLeadingTrivia(originalLeadingTrivia);

            return Task.FromResult<SyntaxNode>(newCompilationUnit);
        }

        return Task.FromResult(root);
    }

    /// <summary>
    /// Checks if a using directive for the specified namespace is present.
    /// </summary>
    /// <param name="compilationUnit">The compilation unit.</param>
    /// <param name="namespaceName">The namespace name to check.</param>
    /// <returns>True if the using directive is present.</returns>
    private static bool HasUsingDirective(CompilationUnitSyntax compilationUnit, string namespaceName)
    {
        return compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName);
    }


    /// <summary>
    /// Gets context-based StringComparison suggestions.
    /// </summary>
    /// <param name="methodName">The string method name.</param>
    /// <param name="invocation">The invocation context.</param>
    /// <returns>Array of suggested StringComparison values.</returns>
    private static string[] GetContextBasedSuggestions(string methodName, InvocationExpressionSyntax invocation)
    {
        // Analyze the invocation to determine appropriate suggestions
        var context = AnalyzeInvocationContext(invocation);

        // Method-specific defaults (based on common usage patterns)
        if (methodName == "Compare")
            return new[] { "StringComparison.Ordinal", "StringComparison.OrdinalIgnoreCase", "StringComparison.CurrentCulture" };

        if (methodName == "EndsWith")
            return new[] { "StringComparison.OrdinalIgnoreCase", "StringComparison.Ordinal", "StringComparison.CurrentCulture" };

        if (methodName == "StartsWith" && context == "General")
            return new[] { "StringComparison.Ordinal", "StringComparison.OrdinalIgnoreCase", "StringComparison.CurrentCulture" };

        return context switch
        {
            "FileExtension" => new[] { "StringComparison.OrdinalIgnoreCase", "StringComparison.Ordinal" },
            "UserText" => new[] { "StringComparison.CurrentCulture", "StringComparison.CurrentCultureIgnoreCase", "StringComparison.Ordinal" },
            "Technical" => new[] { "StringComparison.Ordinal", "StringComparison.OrdinalIgnoreCase" },
            _ => new[] { "StringComparison.Ordinal", "StringComparison.OrdinalIgnoreCase", "StringComparison.CurrentCulture" }
        };
    }

    /// <summary>
    /// Analyzes invocation context to determine the type of string comparison needed.
    /// </summary>
    /// <param name="invocation">The invocation to analyze.</param>
    /// <returns>Context type string.</returns>
    private static string AnalyzeInvocationContext(InvocationExpressionSyntax invocation)
    {
        // Check arguments for file extension patterns
        if (invocation.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = invocation.ArgumentList.Arguments[0];
            if (firstArg.Expression is LiteralExpressionSyntax literal)
            {
                var value = literal.Token.ValueText;
                if (value.StartsWith(".", StringComparison.Ordinal) && value.Length <= 5) // likely file extension
                {
                    return "FileExtension";
                }
            }
        }

        // Check method and variable names for context clues
        var identifiers = invocation.Ancestors().Take(2)
            .SelectMany(ancestor => ancestor.DescendantTokens())
            .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
            .Select(token => token.ValueText.ToLowerInvariant())
            .ToArray();

        if (identifiers.Any(id => id.Contains("user") || id.Contains("display") || id.Contains("message")))
            return "UserText";

        if (identifiers.Any(id => id.Contains("id") || id.Contains("key") || id.Contains("code")))
            return "Technical";

        return "General";
    }

}