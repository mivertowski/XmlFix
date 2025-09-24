using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers;

/// <summary>
/// Analyzer that detects methods that should be properties according to CA1024 rule.
/// Identifies parameterless methods that return values and could be implemented as properties.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PropertyMethodAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic descriptor for methods that should be properties.
    /// </summary>
    private static readonly DiagnosticDescriptor UsePropertyRule = new(
        AnalyzerConstants.UsePropertiesWhereAppropriateId,
        title: "Use properties where appropriate",
        messageFormat: "Method '{0}' should be a property because it takes no parameters and returns a value",
        category: AnalyzerConstants.DesignCategory,
        defaultSeverity: AnalyzerConstants.DefaultSeverity,
        isEnabledByDefault: true,
        description: "Methods that take no parameters and return values should typically be implemented as properties for better API design and performance. Properties are more suitable for data access operations that don't have side effects.",
        helpLinkUri: $"{AnalyzerConstants.MicrosoftDocsBaseUrl}ca1024");

    /// <summary>
    /// Gets the supported diagnostics for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UsePropertyRule);

    /// <summary>
    /// Initializes the analyzer by registering analysis actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    /// <summary>
    /// Analyzes method declarations to determine if they should be properties.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Skip if the method is not a candidate for property conversion
        if (!IsMethodPropertyCandidate(methodDeclaration, semanticModel))
            return;

        // Get the method symbol for additional checks
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol == null)
            return;

        // Skip if this is an override, interface implementation, or virtual method
        if (ShouldSkipMethod(methodSymbol))
            return;

        // Skip if the method has attributes that indicate it shouldn't be a property
        if (HasExcludingAttributes(methodSymbol))
            return;

        // Report the diagnostic
        var location = methodDeclaration.Identifier.GetLocation();
        var diagnostic = Diagnostic.Create(
            UsePropertyRule,
            location,
            methodSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Determines if a method is a candidate for property conversion.
    /// </summary>
    /// <param name="method">The method declaration to analyze.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <returns>True if the method is a property candidate.</returns>
    private static bool IsMethodPropertyCandidate(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        // Use the shared syntax helper for basic checks
        if (!SyntaxHelper.IsPropertyCandidate(method, semanticModel))
            return false;

        // Additional check: ensure the method doesn't access external resources
        return !AccessesExternalResources(method);
    }

    /// <summary>
    /// Checks if a method should be skipped due to inheritance or interface concerns.
    /// </summary>
    /// <param name="method">The method symbol to check.</param>
    /// <returns>True if the method should be skipped.</returns>
    private static bool ShouldSkipMethod(IMethodSymbol method)
    {
        // Skip virtual, abstract, override methods
        if (method.IsVirtual || method.IsAbstract || method.IsOverride)
            return true;

        // Skip interface implementations
        if (method.ExplicitInterfaceImplementations.Length > 0)
            return true;

        // Skip if this implements an interface method
        var containingType = method.ContainingType;
        if (containingType.AllInterfaces
            .SelectMany(iface => iface.GetMembers().OfType<IMethodSymbol>())
            .Any(interfaceMethod => containingType.FindImplementationForInterfaceMember(interfaceMethod)?.Equals(method, SymbolEqualityComparer.Default) == true))
        {
            return true;
        }

        // Skip static methods (they can't be properties in the same way)
        if (method.IsStatic)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a method has attributes that exclude it from property conversion.
    /// </summary>
    /// <param name="method">The method symbol to check.</param>
    /// <returns>True if the method has excluding attributes.</returns>
    private static bool HasExcludingAttributes(IMethodSymbol method)
    {
        var excludingAttributes = new[]
        {
            "ObsoleteAttribute",
            "ConditionalAttribute",
            "MethodImplAttribute",
            "DllImportAttribute",
            "WebMethodAttribute",
            "TestMethodAttribute",
            "FactAttribute",
            "TheoryAttribute"
        };

        return method.GetAttributes()
            .Any(attr => attr.AttributeClass != null &&
                        excludingAttributes.Any(excluding =>
                            attr.AttributeClass.Name == excluding ||
                            attr.AttributeClass.Name == excluding.Replace("Attribute", "")));
    }

    /// <summary>
    /// Checks if a method contains complex logic that suggests it shouldn't be a property.
    /// </summary>
    /// <param name="method">The method declaration to analyze.</param>
    /// <returns>True if the method has complex logic.</returns>
    private static bool HasComplexLogic(MethodDeclarationSyntax method)
    {
        if (method.Body == null)
            return false;

        var complexityIndicators = new SyntaxKind[]
        {
            SyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement,
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement,
            SyntaxKind.TryStatement,
            SyntaxKind.ThrowStatement,
            SyntaxKind.LockStatement,
            SyntaxKind.UsingStatement,
            SyntaxKind.AwaitExpression
        };

        return method.Body.DescendantNodes()
            .Any(node => complexityIndicators.Contains(node.Kind()) ||
                        IsMethodInvocation(node));
    }

    /// <summary>
    /// Checks if a node represents a method invocation that could indicate side effects.
    /// </summary>
    /// <param name="node">The syntax node to check.</param>
    /// <returns>True if the node is a potentially problematic method invocation.</returns>
    private static bool IsMethodInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        // Allow simple property/field access and basic operations
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var memberName = memberAccess.Name.Identifier.ValueText;

            // Allow common safe operations
            var safeMethods = new[]
            {
                "ToString",
                "GetHashCode",
                "Equals",
                "CompareTo",
                "Contains",
                "StartsWith",
                "EndsWith",
                "ToUpper",
                "ToLower",
                "Trim",
                "TrimStart",
                "TrimEnd",
                "Substring"
            };

            if (safeMethods.Contains(memberName))
                return false;

            // Allow property getters (no parentheses in source, but shows as method in syntax)
            return true;
        }

        return true;
    }

    /// <summary>
    /// Checks if a method accesses external resources that make it unsuitable for a property.
    /// </summary>
    /// <param name="method">The method declaration to analyze.</param>
    /// <returns>True if the method accesses external resources.</returns>
    private static bool AccessesExternalResources(MethodDeclarationSyntax method)
    {
        if (method.Body == null && method.ExpressionBody == null)
            return false;

        var resourceAccessIndicators = new[]
        {
            "File",
            "Directory",
            "Path",
            "Stream",
            "Reader",
            "Writer",
            "Database",
            "Connection",
            "Command",
            "Query",
            "Http",
            "Web",
            "Socket",
            "Network",
            "Registry",
            "Environment",
            "Process",
            "Thread",
            "Task",
            "Parallel"
        };

        var allNodes = method.Body?.DescendantNodes() ??
                      method.ExpressionBody?.DescendantNodes() ??
                      Enumerable.Empty<SyntaxNode>();

        return allNodes.OfType<IdentifierNameSyntax>()
            .Any(identifier => resourceAccessIndicators
                .Any(indicator => identifier.Identifier.ValueText.IndexOf(indicator, System.StringComparison.OrdinalIgnoreCase) >= 0));
    }
}