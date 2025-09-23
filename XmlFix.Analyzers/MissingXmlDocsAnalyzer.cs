using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace XmlFix.Analyzers;

/// <summary>
/// Analyzer that detects public members missing XML documentation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingXmlDocsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for missing XML documentation.
    /// </summary>
    public const string MissingXmlDocsId = "XDOC001";

    /// <summary>
    /// Diagnostic descriptor for missing XML documentation.
    /// </summary>
    private static readonly DiagnosticDescriptor MissingXmlDocsRule = new(
        MissingXmlDocsId,
        title: "Missing XML documentation",
        messageFormat: "Public {0} '{1}' is missing XML documentation",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Public members should have XML documentation comments to improve code maintainability and API usability.",
        helpLinkUri: "https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/");

    /// <summary>
    /// Gets the supported diagnostics for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingXmlDocsRule);

    /// <summary>
    /// Initializes the analyzer by registering analysis actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeSymbol,
            SymbolKind.NamedType,
            SymbolKind.Method,
            SymbolKind.Property,
            SymbolKind.Event,
            SymbolKind.Field,
            SymbolKind.Namespace);
    }

    /// <summary>
    /// Analyzes a symbol to determine if it needs XML documentation.
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;

        // Skip if not public or is compiler generated
        if (symbol.DeclaredAccessibility != Accessibility.Public ||
            symbol.IsImplicitlyDeclared ||
            IsCompilerGenerated(symbol))
        {
            return;
        }

        // Skip namespaces - they don't need XML docs
        if (symbol.Kind == SymbolKind.Namespace)
        {
            return;
        }

        // Skip non-const fields that are not public instance fields
        if (symbol is IFieldSymbol field && !field.IsConst && field.IsStatic)
        {
            return;
        }

        // Skip property accessors - they are documented with the property itself
        if (symbol is IMethodSymbol method &&
            (method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet))
        {
            return;
        }

        // Skip if already has documentation
        if (HasXmlDocumentation(symbol, context.CancellationToken))
        {
            return;
        }

        // Skip if this is an override or interface implementation that should use inheritdoc
        if (ShouldUseInheritdoc(symbol))
        {
            return;
        }

        // Report diagnostic for missing documentation
        var location = GetSymbolLocation(symbol);
        if (location != null)
        {
            var memberType = GetMemberTypeName(symbol);
            var diagnostic = Diagnostic.Create(
                MissingXmlDocsRule,
                location,
                memberType,
                symbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Determines if a symbol is compiler generated.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True if the symbol is compiler generated.</returns>
    private static bool IsCompilerGenerated(ISymbol symbol)
    {
        // Check for CompilerGenerated attribute
        return symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "CompilerGeneratedAttribute");
    }

    /// <summary>
    /// Checks if a symbol already has XML documentation.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the symbol has XML documentation.</returns>
    private static bool HasXmlDocumentation(ISymbol symbol, System.Threading.CancellationToken cancellationToken)
    {
        return symbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(cancellationToken))
            .Any(syntax => HasDocumentationComment(syntax));
    }

    /// <summary>
    /// Checks if a syntax node has documentation comments.
    /// </summary>
    /// <param name="syntax">The syntax node to check.</param>
    /// <returns>True if the node has documentation comments.</returns>
    private static bool HasDocumentationComment(SyntaxNode syntax)
    {
        var leadingTrivia = syntax.GetLeadingTrivia();
        return leadingTrivia.Any(trivia =>
            trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

    /// <summary>
    /// Determines if a symbol should use inheritdoc instead of regular documentation.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>True if the symbol should use inheritdoc.</returns>
    private static bool ShouldUseInheritdoc(ISymbol symbol)
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
    /// Gets the primary location of a symbol for diagnostic reporting.
    /// </summary>
    /// <param name="symbol">The symbol to get the location for.</param>
    /// <returns>The primary location of the symbol, or null if not available.</returns>
    private static Location? GetSymbolLocation(ISymbol symbol)
    {
        return symbol.Locations.FirstOrDefault(location => location.IsInSource);
    }

    /// <summary>
    /// Gets a user-friendly name for the member type.
    /// </summary>
    /// <param name="symbol">The symbol to get the type name for.</param>
    /// <returns>A user-friendly name for the member type.</returns>
    private static string GetMemberTypeName(ISymbol symbol)
    {
        return symbol.Kind switch
        {
            SymbolKind.NamedType => symbol switch
            {
                INamedTypeSymbol { TypeKind: TypeKind.Class } => "class",
                INamedTypeSymbol { TypeKind: TypeKind.Interface } => "interface",
                INamedTypeSymbol { TypeKind: TypeKind.Struct } => "struct",
                INamedTypeSymbol { TypeKind: TypeKind.Enum } => "enum",
                INamedTypeSymbol { TypeKind: TypeKind.Delegate } => "delegate",
                _ => "type"
            },
            SymbolKind.Method => symbol switch
            {
                IMethodSymbol { MethodKind: MethodKind.Constructor } => "constructor",
                IMethodSymbol { MethodKind: MethodKind.Destructor } => "destructor",
                IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } => "operator",
                _ => "method"
            },
            SymbolKind.Property => "property",
            SymbolKind.Field => "field",
            SymbolKind.Event => "event",
            _ => "member"
        };
    }
}