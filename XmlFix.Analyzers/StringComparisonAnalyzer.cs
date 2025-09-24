using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers;

/// <summary>
/// Analyzer that detects string comparison operations that should specify StringComparison according to CA1310 rule.
/// Identifies calls to string comparison methods without explicit StringComparison parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringComparisonAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic descriptor for string comparison without StringComparison parameter.
    /// </summary>
    private static readonly DiagnosticDescriptor SpecifyStringComparisonRule = new(
        AnalyzerConstants.SpecifyStringComparisonId,
        title: "Specify StringComparison for string operations",
        messageFormat: "String.{0} should specify StringComparison to be explicit about cultural assumptions",
        category: AnalyzerConstants.UsageCategory,
        defaultSeverity: AnalyzerConstants.DefaultSeverity,
        isEnabledByDefault: true,
        description: "String comparison operations should explicitly specify StringComparison to avoid unintended behavior due to cultural differences. This makes the code more predictable and prevents subtle bugs related to string comparison.",
        helpLinkUri: $"{AnalyzerConstants.MicrosoftDocsBaseUrl}ca1310");

    /// <summary>
    /// Gets the supported diagnostics for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(SpecifyStringComparisonRule);

    /// <summary>
    /// Initializes the analyzer by registering analysis actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    /// <summary>
    /// Analyzes invocation expressions to find string comparison methods without StringComparison.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Check if this is a string comparison method call that needs StringComparison
        if (!SyntaxHelper.IsStringComparisonMethodCall(invocation, semanticModel))
            return;

        // Get method information
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.ValueText;
        var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

        if (methodSymbol == null)
            return;

        // Verify this is actually a string method from System.String
        if (!IsSystemStringMethod(methodSymbol))
            return;

        // Check if there's an overload that accepts StringComparison
        if (!HasStringComparisonOverload(methodSymbol))
            return;

        // Don't skip culture-specific contexts - we should still report them
        // The code fix will offer appropriate StringComparison options including CurrentCulture

        // Report the diagnostic
        var location = memberAccess.Name.GetLocation();
        var diagnostic = Diagnostic.Create(
            SpecifyStringComparisonRule,
            location,
            methodName);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Checks if the method symbol represents a System.String method.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to check.</param>
    /// <returns>True if this is a System.String method.</returns>
    private static bool IsSystemStringMethod(IMethodSymbol methodSymbol)
    {
        return methodSymbol.ContainingType?.SpecialType == SpecialType.System_String;
    }

    /// <summary>
    /// Checks if the string method has an overload that accepts StringComparison.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to check.</param>
    /// <returns>True if there's a StringComparison overload.</returns>
    private static bool HasStringComparisonOverload(IMethodSymbol methodSymbol)
    {
        var methodName = methodSymbol.Name;
        var containingType = methodSymbol.ContainingType;

        if (containingType == null)
            return false;

        // Look for overloads of the same method that accept StringComparison
        var overloads = containingType.GetMembers(methodName).OfType<IMethodSymbol>();

        return overloads.Any(overload =>
            overload.Parameters.Any(param =>
                param.Type.Name == "StringComparison" &&
                param.Type.ContainingNamespace?.ToDisplayString() == "System"));
    }

    /// <summary>
    /// Checks if the invocation is in a context where culture-specific comparison might be intended.
    /// </summary>
    /// <param name="invocation">The invocation expression.</param>
    /// <returns>True if culture-specific comparison might be intended.</returns>
    private static bool IsInCultureSpecificContext(InvocationExpressionSyntax invocation)
    {
        // Look for patterns that suggest culture-specific operations
        var parentNodes = invocation.Ancestors().Take(5);

        foreach (var ancestor in parentNodes)
        {
            // Check for culture-related identifiers
            var identifiers = ancestor.DescendantTokens()
                .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
                .Select(token => token.ValueText);

            var cultureIndicators = new[]
            {
                "Culture",
                "CultureInfo",
                "Locale",
                "Localization",
                "CurrentCulture",
                "InvariantCulture",
                "Globalization",
                "Sort",
                "Collate",
                "Display",
                "User"
            };

            if (identifiers.Any(id => cultureIndicators.Any(indicator =>
                id.IndexOf(indicator, System.StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                return true;
            }

            // Check for method names that suggest UI or user-facing operations
            if (ancestor is MethodDeclarationSyntax method)
            {
                var methodName = method.Identifier.ValueText;
                var uiMethodIndicators = new[]
                {
                    "Display",
                    "Show",
                    "Render",
                    "Present",
                    "UI",
                    "User"
                };

                if (uiMethodIndicators.Any(indicator =>
                    methodName.IndexOf(indicator, System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Gets suggested StringComparison values based on context analysis.
    /// </summary>
    /// <param name="invocation">The invocation expression.</param>
    /// <param name="methodName">The method name being called.</param>
    /// <returns>Suggested StringComparison values in order of preference.</returns>
    public static string[] GetSuggestedStringComparisons(InvocationExpressionSyntax invocation, string methodName)
    {
        // Analyze context to suggest appropriate StringComparison
        var contextAnalysis = AnalyzeContext(invocation);

        return contextAnalysis switch
        {
            ContextType.Identifier or ContextType.TechnicalData => new[]
            {
                "StringComparison.Ordinal",
                "StringComparison.OrdinalIgnoreCase"
            },
            ContextType.UserData or ContextType.DisplayText => new[]
            {
                "StringComparison.CurrentCulture",
                "StringComparison.CurrentCultureIgnoreCase",
                "StringComparison.Ordinal"
            },
            ContextType.Configuration or ContextType.Parsing => new[]
            {
                "StringComparison.OrdinalIgnoreCase",
                "StringComparison.Ordinal"
            },
            _ => new[]
            {
                "StringComparison.Ordinal",
                "StringComparison.OrdinalIgnoreCase",
                "StringComparison.CurrentCulture"
            }
        };
    }

    /// <summary>
    /// Types of contexts for string comparison operations.
    /// </summary>
    private enum ContextType
    {
        Unknown,
        Identifier,
        TechnicalData,
        UserData,
        DisplayText,
        Configuration,
        Parsing
    }

    /// <summary>
    /// Analyzes the context of a string comparison to suggest appropriate StringComparison.
    /// </summary>
    /// <param name="invocation">The invocation expression.</param>
    /// <returns>The detected context type.</returns>
    private static ContextType AnalyzeContext(InvocationExpressionSyntax invocation)
    {
        // Look at variable names and method context to determine likely usage
        var nearbyIdentifiers = invocation.Ancestors().Take(3)
            .SelectMany(ancestor => ancestor.DescendantTokens())
            .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
            .Select(token => token.ValueText.ToLowerInvariant())
            .ToList();

        // Check for identifier/technical data patterns
        var identifierPatterns = new[] { "id", "guid", "key", "name", "type", "kind", "code", "token", "hash" };
        if (nearbyIdentifiers.Any(id => identifierPatterns.Any(pattern => id.Contains(pattern))))
        {
            return ContextType.Identifier;
        }

        // Check for user data patterns
        var userDataPatterns = new[] { "user", "display", "text", "message", "title", "description", "label" };
        if (nearbyIdentifiers.Any(id => userDataPatterns.Any(pattern => id.Contains(pattern))))
        {
            return ContextType.UserData;
        }

        // Check for configuration patterns
        var configPatterns = new[] { "config", "setting", "option", "parameter", "property", "attribute" };
        if (nearbyIdentifiers.Any(id => configPatterns.Any(pattern => id.Contains(pattern))))
        {
            return ContextType.Configuration;
        }

        // Check for parsing patterns
        var parsePatterns = new[] { "parse", "format", "convert", "transform", "process" };
        if (nearbyIdentifiers.Any(id => parsePatterns.Any(pattern => id.Contains(pattern))))
        {
            return ContextType.Parsing;
        }

        return ContextType.Unknown;
    }
}