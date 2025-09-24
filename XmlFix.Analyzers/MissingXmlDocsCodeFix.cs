using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XmlFix.Analyzers;

/// <summary>
/// Code fix provider for adding XML documentation to members missing it.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingXmlDocsCodeFix)), Shared]
public sealed class MissingXmlDocsCodeFix : CodeFixProvider
{
    /// <summary>
    /// Gets the diagnostic IDs that this code fix provider can fix.
    /// </summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(MissingXmlDocsAnalyzer.MissingXmlDocsId);

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

        var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == MissingXmlDocsAnalyzer.MissingXmlDocsId);
        if (diagnostic == null) return;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        if (node == null) return;

        var declaration = FindDeclaration(node);
        if (declaration == null) return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return;

        // For fields, we need to get the symbol from the variable declarator
        ISymbol? symbol = null;
        if (declaration is BaseFieldDeclarationSyntax fieldDecl)
        {
            var variableDeclarator = fieldDecl.Declaration.Variables.FirstOrDefault();
            if (variableDeclarator != null)
            {
                symbol = semanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken);
            }
        }
        else
        {
            symbol = semanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
        }

        if (symbol == null) return;

        // Register code action for adding XML documentation
        var addDocsAction = CodeAction.Create(
            title: "Add XML documentation",
            createChangedDocument: c => AddXmlDocumentationAsync(context.Document, declaration, symbol, inheritDoc: false, c),
            equivalenceKey: "AddXmlDocs");

        context.RegisterCodeFix(addDocsAction, diagnostic);

        // Register code action for adding inheritdoc if applicable
        if (ShouldOfferInheritdoc(symbol))
        {
            var addInheritdocAction = CodeAction.Create(
                title: "Add <inheritdoc/>",
                createChangedDocument: c => AddXmlDocumentationAsync(context.Document, declaration, symbol, inheritDoc: true, c),
                equivalenceKey: "AddInheritdoc");

            context.RegisterCodeFix(addInheritdocAction, diagnostic);
        }
    }

    /// <summary>
    /// Finds the declaration node that should receive documentation.
    /// </summary>
    /// <param name="node">The syntax node to start searching from.</param>
    /// <returns>The declaration node, or null if not found.</returns>
    private static SyntaxNode? FindDeclaration(SyntaxNode node)
    {
        return node.FirstAncestorOrSelf<SyntaxNode>(n =>
            n is BaseMethodDeclarationSyntax ||
            n is BasePropertyDeclarationSyntax ||
            n is BaseTypeDeclarationSyntax ||
            n is DelegateDeclarationSyntax ||
            n is BaseFieldDeclarationSyntax ||
            n is EventFieldDeclarationSyntax ||
            n is EventDeclarationSyntax);
    }

    /// <summary>
    /// Determines if inheritdoc should be offered as an option for this symbol.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True if inheritdoc should be offered.</returns>
    private static bool ShouldOfferInheritdoc(ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol method => method.IsOverride ||
                                  method.ExplicitInterfaceImplementations.Length > 0 ||
                                  IsInterfaceImplementation(method),
            IPropertySymbol property => property.IsOverride ||
                                      property.ExplicitInterfaceImplementations.Length > 0 ||
                                      IsInterfaceImplementation(property),
            IEventSymbol evt => evt.IsOverride ||
                               evt.ExplicitInterfaceImplementations.Length > 0 ||
                               IsInterfaceImplementation(evt),
            _ => false
        };
    }

    /// <summary>
    /// Checks if a method implements an interface method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True if the method implements an interface method.</returns>
    private static bool IsInterfaceImplementation(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        return containingType.AllInterfaces
            .SelectMany(iface => iface.GetMembers().OfType<IMethodSymbol>())
            .Any(interfaceMethod => containingType.FindImplementationForInterfaceMember(interfaceMethod)?.Equals(method, SymbolEqualityComparer.Default) == true);
    }

    /// <summary>
    /// Checks if a property implements an interface property.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns>True if the property implements an interface property.</returns>
    private static bool IsInterfaceImplementation(IPropertySymbol property)
    {
        var containingType = property.ContainingType;
        return containingType.AllInterfaces
            .SelectMany(iface => iface.GetMembers().OfType<IPropertySymbol>())
            .Any(interfaceProperty => containingType.FindImplementationForInterfaceMember(interfaceProperty)?.Equals(property, SymbolEqualityComparer.Default) == true);
    }

    /// <summary>
    /// Checks if an event implements an interface event.
    /// </summary>
    /// <param name="evt">The event to check.</param>
    /// <returns>True if the event implements an interface event.</returns>
    private static bool IsInterfaceImplementation(IEventSymbol evt)
    {
        var containingType = evt.ContainingType;
        return containingType.AllInterfaces
            .SelectMany(iface => iface.GetMembers().OfType<IEventSymbol>())
            .Any(interfaceEvent => containingType.FindImplementationForInterfaceMember(interfaceEvent)?.Equals(evt, SymbolEqualityComparer.Default) == true);
    }

    /// <summary>
    /// Adds XML documentation to the specified declaration.
    /// </summary>
    /// <param name="document">The document containing the declaration.</param>
    /// <param name="declaration">The declaration to add documentation to.</param>
    /// <param name="symbol">The symbol representing the declaration.</param>
    /// <param name="inheritDoc">Whether to use inheritdoc instead of full documentation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated document.</returns>
    private static async Task<Document> AddXmlDocumentationAsync(
        Document document,
        SyntaxNode declaration,
        ISymbol symbol,
        bool inheritDoc,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Get the indentation from the declaration
        var leadingTrivia = declaration.GetLeadingTrivia();
        var indentation = GetIndentation(leadingTrivia);

        var documentationText = inheritDoc
            ? GenerateInheritdocComment(indentation)
            : GenerateXmlDocumentationStub(declaration, symbol, indentation);

        var documentationTrivia = SyntaxFactory.ParseLeadingTrivia(documentationText);
        var newDeclaration = declaration.WithLeadingTrivia(documentationTrivia.AddRange(declaration.GetLeadingTrivia()));

        var newRoot = root.ReplaceNode(declaration, newDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Gets the indentation from the leading trivia.
    /// </summary>
    /// <param name="leadingTrivia">The leading trivia of the declaration.</param>
    /// <returns>The indentation string.</returns>
    private static string GetIndentation(SyntaxTriviaList leadingTrivia)
    {
        foreach (var trivia in leadingTrivia.Reverse())
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                return trivia.ToString();
            }
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                break;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Generates an inheritdoc comment.
    /// </summary>
    /// <param name="indentation">The indentation to use.</param>
    /// <returns>The inheritdoc comment text.</returns>
    private static string GenerateInheritdocComment(string indentation)
    {
        return $"{indentation}/// <inheritdoc/>\n";
    }

    /// <summary>
    /// Generates a complete XML documentation stub for the specified declaration.
    /// </summary>
    /// <param name="declaration">The declaration to generate documentation for.</param>
    /// <param name="symbol">The symbol representing the declaration.</param>
    /// <param name="indentation">The indentation to use.</param>
    /// <returns>The XML documentation stub.</returns>
    private static string GenerateXmlDocumentationStub(SyntaxNode declaration, ISymbol symbol, string indentation)
    {
        var builder = new StringBuilder();

        // Generate summary
        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);
        builder.AppendLine($"{indentation}/// <summary>");
        builder.AppendLine($"{indentation}/// {summary}");
        builder.AppendLine($"{indentation}/// </summary>");

        // Add type parameters for generic types and methods
        if (symbol is INamedTypeSymbol namedType && namedType.TypeParameters.Length > 0)
        {
            foreach (var typeParam in namedType.TypeParameters)
            {
                builder.AppendLine($"{indentation}/// <typeparam name=\"{typeParam.Name}\">The {typeParam.Name} type parameter.</typeparam>");
            }
        }
        else if (symbol is IMethodSymbol method && method.TypeParameters.Length > 0)
        {
            foreach (var typeParam in method.TypeParameters)
            {
                builder.AppendLine($"{indentation}/// <typeparam name=\"{typeParam.Name}\">The {typeParam.Name} type parameter.</typeparam>");
            }
        }

        // Add parameters for methods, constructors, and indexers
        if (declaration is MethodDeclarationSyntax methodDecl)
        {
            foreach (var param in methodDecl.ParameterList.Parameters)
            {
                var paramName = param.Identifier.ValueText;
                var paramDescription = DocumentationGenerator.GenerateParameterDescription(paramName);
                builder.AppendLine($"{indentation}/// <param name=\"{paramName}\">{paramDescription}</param>");
            }

            // Add returns for non-void methods
            if (!(methodDecl.ReturnType is PredefinedTypeSyntax predefined &&
                  predefined.Keyword.IsKind(SyntaxKind.VoidKeyword)))
            {
                var returnDescription = DocumentationGenerator.GenerateReturnDescription(symbol);
                builder.AppendLine($"{indentation}/// <returns>{returnDescription}</returns>");
            }
        }
        else if (declaration is ConstructorDeclarationSyntax ctorDecl)
        {
            foreach (var param in ctorDecl.ParameterList.Parameters)
            {
                var paramName = param.Identifier.ValueText;
                var paramDescription = DocumentationGenerator.GenerateParameterDescription(paramName);
                builder.AppendLine($"{indentation}/// <param name=\"{paramName}\">{paramDescription}</param>");
            }
        }
        else if (declaration is IndexerDeclarationSyntax indexerDecl)
        {
            foreach (var param in indexerDecl.ParameterList.Parameters)
            {
                var paramName = param.Identifier.ValueText;
                var paramDescription = DocumentationGenerator.GenerateParameterDescription(paramName);
                builder.AppendLine($"{indentation}/// <param name=\"{paramName}\">{paramDescription}</param>");
            }

            builder.AppendLine("/// <returns>The value at the specified index.</returns>");
        }
        else if (declaration is DelegateDeclarationSyntax delegateDecl)
        {
            foreach (var param in delegateDecl.ParameterList.Parameters)
            {
                var paramName = param.Identifier.ValueText;
                var paramDescription = DocumentationGenerator.GenerateParameterDescription(paramName);
                builder.AppendLine($"{indentation}/// <param name=\"{paramName}\">{paramDescription}</param>");
            }

            if (!(delegateDecl.ReturnType is PredefinedTypeSyntax predefined &&
                  predefined.Keyword.IsKind(SyntaxKind.VoidKeyword)))
            {
                var returnDescription = DocumentationGenerator.GenerateReturnDescription(symbol);
                builder.AppendLine($"{indentation}/// <returns>{returnDescription}</returns>");
            }
        }
        else if (declaration is BaseFieldDeclarationSyntax)
        {
            // Fields don't need additional XML tags beyond summary
            // The summary was already added above
        }
        else if (declaration is EventFieldDeclarationSyntax || declaration is EventDeclarationSyntax)
        {
            // Events don't need additional XML tags beyond summary
            // The summary was already added above
        }

        // Add value for properties and indexers
        if (declaration is PropertyDeclarationSyntax || declaration is IndexerDeclarationSyntax)
        {
            if (!(declaration is IndexerDeclarationSyntax)) // Already handled above for indexers
            {
                var valueDescription = DocumentationGenerator.GenerateValueDescription(symbol);
                builder.AppendLine($"{indentation}/// <value>{valueDescription}</value>");
            }
        }

        return builder.ToString();
    }
}