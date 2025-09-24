using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers;

/// <summary>
/// Analyzer that detects ILogger extension method calls that should use LoggerMessage delegates according to CA1848 rule.
/// Identifies high-performance logging scenarios where LoggerMessage pattern would be more efficient.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoggerMessageAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic descriptor for logger calls that should use LoggerMessage pattern.
    /// </summary>
    private static readonly DiagnosticDescriptor UseLoggerMessageRule = new(
        AnalyzerConstants.UseLoggerMessageDelegatesId,
        title: "Use LoggerMessage delegates for high-performance logging",
        messageFormat: "Use LoggerMessage.Define for the logging call '{0}' to improve performance",
        category: AnalyzerConstants.PerformanceCategory,
        defaultSeverity: AnalyzerConstants.DefaultSeverity,
        isEnabledByDefault: true,
        description: "LoggerMessage delegates provide better performance for high-frequency logging by avoiding boxing, allocations, and string formatting when the log level is disabled. Use LoggerMessage.Define to create compiled delegates for frequently used log messages.",
        helpLinkUri: $"{AnalyzerConstants.MicrosoftDocsBaseUrl}ca1848");

    /// <summary>
    /// Gets the supported diagnostics for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UseLoggerMessageRule);

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
    /// Analyzes invocation expressions to find logger extension method calls.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Check if this is a logger extension method call
        if (!SyntaxHelper.IsLoggerExtensionMethodCall(invocation, semanticModel))
            return;

        // Get method information
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.ValueText;
        var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

        if (methodSymbol == null)
            return;

        // Skip if this is already using LoggerMessage pattern
        if (IsAlreadyUsingLoggerMessage(invocation))
            return;

        // Be permissive: Any logger call with formatting should trigger the diagnostic
        // This ensures we catch all cases that could benefit from LoggerMessage
        if (!HasAnyFormatting(invocation))
            return;

        // Report the diagnostic
        var location = memberAccess.Name.GetLocation();
        var diagnostic = Diagnostic.Create(
            UseLoggerMessageRule,
            location,
            methodName);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Checks if the invocation is already using LoggerMessage pattern.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns>True if already using LoggerMessage pattern.</returns>
    private static bool IsAlreadyUsingLoggerMessage(InvocationExpressionSyntax invocation)
    {
        // Look for LoggerMessage.Define in the containing class or nearby
        var containingClass = invocation.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (containingClass == null)
            return false;

        // Check for fields or properties that use LoggerMessage.Define
        var loggerMessageFields = containingClass.Members
            .OfType<FieldDeclarationSyntax>()
            .Where(field => field.ToString().Contains("LoggerMessage.Define"));

        var loggerMessageProperties = containingClass.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => property.ToString().Contains("LoggerMessage.Define"));

        return loggerMessageFields.Any() || loggerMessageProperties.Any();
    }

    /// <summary>
    /// Determines if the invocation is in a performance-critical context.
    /// </summary>
    /// <param name="invocation">The invocation to analyze.</param>
    /// <returns>True if in a performance-critical context.</returns>
    private static bool IsInPerformanceCriticalContext(InvocationExpressionSyntax invocation)
    {
        // Look for performance-critical indicators in the surrounding context
        var containingMethod = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (containingMethod == null)
            return false;

        var methodName = containingMethod.Identifier.ValueText.ToLowerInvariant();
        var performanceCriticalIndicators = new[]
        {
            "loop", "foreach", "for", "while", "process", "handle", "execute",
            "async", "task", "parallel", "concurrent", "batch", "bulk",
            "hot", "critical", "fast", "performance", "optimized"
        };

        // Check method name
        if (performanceCriticalIndicators.Any(indicator => methodName.Contains(indicator)))
            return true;

        // Check if inside a loop
        var containingLoop = invocation.FirstAncestorOrSelf<SyntaxNode>(node =>
            node.IsKind(SyntaxKind.ForStatement) ||
            node.IsKind(SyntaxKind.ForEachStatement) ||
            node.IsKind(SyntaxKind.WhileStatement) ||
            node.IsKind(SyntaxKind.DoStatement));

        if (containingLoop != null)
            return true;

        // Check for async/await context
        if (containingMethod.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
            return true;

        // Check for high-frequency operation patterns
        var containingClass = invocation.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (containingClass != null)
        {
            var className = containingClass.Identifier.ValueText.ToLowerInvariant();
            var highFrequencyClassIndicators = new[]
            {
                "processor", "handler", "worker", "service", "engine",
                "manager", "controller", "middleware", "interceptor"
            };

            if (highFrequencyClassIndicators.Any(indicator => className.Contains(indicator)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if LoggerMessage should always be used for this logging call regardless of context.
    /// </summary>
    /// <param name="invocation">The invocation to analyze.</param>
    /// <returns>True if LoggerMessage should always be used.</returns>
    private static bool ShouldAlwaysUseLoggerMessage(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return false;

        var messageArgument = invocation.ArgumentList.Arguments[0];

        // Any string interpolation, concatenation, or formatting should use LoggerMessage
        return messageArgument.Expression is InterpolatedStringExpressionSyntax ||
               messageArgument.Expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression) ||
               messageArgument.Expression is InvocationExpressionSyntax inv && (IsStringFormatCall(inv) || inv.ToString().Contains("string.Format"));
    }

    /// <summary>
    /// Checks if the invocation contains any form of string formatting.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns>True if the invocation contains formatting.</returns>
    private static bool ContainsFormatting(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return false;

        var messageArgument = invocation.ArgumentList.Arguments[0];
        var messageText = messageArgument.ToString();

        // Check for various formatting patterns
        return messageText.Contains("string.Format") ||
               messageText.Contains("String.Format") ||
               messageText.Contains("$\"") ||
               messageText.Contains("\" +") ||
               messageText.Contains("+ \"");
    }

    /// <summary>
    /// Simplified check for any type of formatting that would benefit from LoggerMessage.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns>True if the invocation has any formatting.</returns>
    private static bool HasAnyFormatting(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return false;

        var messageArgument = invocation.ArgumentList.Arguments[0];

        // Check for any kind of formatting
        return messageArgument.Expression is InterpolatedStringExpressionSyntax ||
               messageArgument.Expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression) ||
               messageArgument.Expression is InvocationExpressionSyntax inv && IsAnyFormatCall(inv) ||
               messageArgument.ToString().Contains("string.Format") ||
               messageArgument.ToString().Contains("String.Format") ||
               invocation.ArgumentList.Arguments.Count > 1; // Multiple parameters suggest formatting
    }

    /// <summary>
    /// Checks if an invocation is any kind of formatting call (String.Format, etc.).
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns>True if this is a formatting call.</returns>
    private static bool IsAnyFormatCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.ValueText;
            var targetName = memberAccess.Expression.ToString();

            return (methodName == "Format" && (targetName == "string" || targetName == "String")) ||
                   methodName.Contains("Format");
        }

        return false;
    }

    /// <summary>
    /// Determines if the logging call would benefit from LoggerMessage pattern.
    /// </summary>
    /// <param name="invocation">The invocation to analyze.</param>
    /// <returns>True if the call would benefit from LoggerMessage.</returns>
    private static bool WouldBenefitFromLoggerMessage(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return false;

        var messageArgument = invocation.ArgumentList.Arguments[0];

        // Any string interpolation, concatenation, or formatting benefits from LoggerMessage
        return messageArgument.Expression is InterpolatedStringExpressionSyntax ||
               messageArgument.Expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression) ||
               HasComplexMessageFormatting(messageArgument.Expression) ||
               HasMultipleParameters(invocation) ||
               UsesExpensiveOperations(messageArgument.Expression);
    }

    /// <summary>
    /// Checks if the message uses complex formatting that would benefit from LoggerMessage.
    /// </summary>
    /// <param name="messageExpression">The message expression to analyze.</param>
    /// <returns>True if the message has complex formatting.</returns>
    private static bool HasComplexMessageFormatting(ExpressionSyntax messageExpression)
    {
        return messageExpression switch
        {
            // String interpolation with multiple parameters
            InterpolatedStringExpressionSyntax interpolated =>
                interpolated.Contents.OfType<InterpolationSyntax>().Count() > 1,

            // Binary expression (string concatenation)
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) =>
                CountConcatenationOperands(binary) > 2,

            // String.Format calls
            InvocationExpressionSyntax invocation when IsStringFormatCall(invocation) =>
                invocation.ArgumentList.Arguments.Count > 2,

            _ => false
        };
    }

    /// <summary>
    /// Checks if the invocation has multiple parameters that could be optimized.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns>True if there are multiple parameters.</returns>
    private static bool HasMultipleParameters(InvocationExpressionSyntax invocation)
    {
        return invocation.ArgumentList.Arguments.Count > 2; // Logger, message, and additional parameters
    }

    /// <summary>
    /// Checks if the message expression uses expensive operations.
    /// </summary>
    /// <param name="messageExpression">The message expression to analyze.</param>
    /// <returns>True if expensive operations are used.</returns>
    private static bool UsesExpensiveOperations(ExpressionSyntax messageExpression)
    {
        var expensiveOperations = messageExpression.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(inv => GetMethodName(inv))
            .Where(name => name != null);

        var expensiveMethodNames = new[]
        {
            "ToString", "Format", "Join", "Concat", "Replace",
            "Substring", "ToUpper", "ToLower", "Trim"
        };

        return expensiveOperations.Any(name =>
            expensiveMethodNames.Any(expensive =>
                name!.IndexOf(expensive, System.StringComparison.OrdinalIgnoreCase) >= 0));
    }

    /// <summary>
    /// Counts the number of operands in a string concatenation expression.
    /// </summary>
    /// <param name="binaryExpression">The binary expression to analyze.</param>
    /// <returns>The number of concatenation operands.</returns>
    private static int CountConcatenationOperands(BinaryExpressionSyntax binaryExpression)
    {
        var count = 0;

        void CountOperands(ExpressionSyntax expression)
        {
            if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
            {
                CountOperands(binary.Left);
                CountOperands(binary.Right);
            }
            else
            {
                count++;
            }
        }

        CountOperands(binaryExpression);
        return count;
    }

    /// <summary>
    /// Checks if an invocation is a String.Format call.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns>True if this is a String.Format call.</returns>
    private static bool IsStringFormatCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.ValueText == "Format" &&
                   memberAccess.Expression is IdentifierNameSyntax identifier &&
                   (identifier.Identifier.ValueText == "string" || identifier.Identifier.ValueText == "String");
        }

        return false;
    }

    /// <summary>
    /// Checks if an invocation is a String.Format call using semantic analysis.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <returns>True if this is a String.Format call.</returns>
    private static bool IsStringFormatCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is IMethodSymbol method)
        {
            return method.Name == "Format" &&
                   method.ContainingType?.Name == "String" &&
                   method.ContainingType.ContainingNamespace?.ToDisplayString() == "System";
        }

        return IsStringFormatCall(invocation);
    }

    /// <summary>
    /// Gets the method name from an invocation expression.
    /// </summary>
    /// <param name="invocation">The invocation expression.</param>
    /// <returns>The method name, or null if not determinable.</returns>
    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null
        };
    }

    /// <summary>
    /// Gets information about the logging call for code fix purposes.
    /// </summary>
    /// <param name="invocation">The logging invocation.</param>
    /// <returns>Information about the logging call.</returns>
    public static LoggerCallInfo AnalyzeLoggerCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return new LoggerCallInfo();

        var methodName = memberAccess.Name.Identifier.ValueText;
        var logLevel = GetLogLevelFromMethodName(methodName);
        var parameters = ExtractParametersFromMessage(invocation);

        return new LoggerCallInfo(
            methodName,
            logLevel,
            parameters,
            ExtractMessageTemplate(invocation),
            HasExceptionParameter(invocation));
    }

    /// <summary>
    /// Information about a logger method call.
    /// </summary>
    public struct LoggerCallInfo
    {
        public LoggerCallInfo(string methodName = "", string logLevel = "", string[] parameters = null!, string messageTemplate = "", bool hasException = false)
        {
            MethodName = methodName ?? "";
            LogLevel = logLevel ?? "";
            Parameters = parameters ?? System.Array.Empty<string>();
            MessageTemplate = messageTemplate ?? "";
            HasException = hasException;
        }

        public string MethodName { get; }
        public string LogLevel { get; }
        public string[] Parameters { get; }
        public string MessageTemplate { get; }
        public bool HasException { get; }
    }

    /// <summary>
    /// Extracts the log level from a logger method name.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <returns>The log level.</returns>
    private static string GetLogLevelFromMethodName(string methodName)
    {
        return methodName switch
        {
            "LogTrace" => "LogLevel.Trace",
            "LogDebug" => "LogLevel.Debug",
            "LogInformation" => "LogLevel.Information",
            "LogWarning" => "LogLevel.Warning",
            "LogError" => "LogLevel.Error",
            "LogCritical" => "LogLevel.Critical",
            _ => "LogLevel.Information"
        };
    }

    /// <summary>
    /// Extracts parameter information from a logging message.
    /// </summary>
    /// <param name="invocation">The logging invocation.</param>
    /// <returns>Array of parameter names.</returns>
    private static string[] ExtractParametersFromMessage(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return System.Array.Empty<string>();

        var messageArgument = invocation.ArgumentList.Arguments[0];

        return messageArgument.Expression switch
        {
            InterpolatedStringExpressionSyntax interpolated =>
                ExtractParametersFromInterpolation(interpolated),
            _ => System.Array.Empty<string>()
        };
    }

    /// <summary>
    /// Extracts parameters from an interpolated string expression.
    /// </summary>
    /// <param name="interpolated">The interpolated string.</param>
    /// <returns>Array of parameter names.</returns>
    private static string[] ExtractParametersFromInterpolation(InterpolatedStringExpressionSyntax interpolated)
    {
        return interpolated.Contents
            .OfType<InterpolationSyntax>()
            .Select((interpolation, index) => GetParameterName(interpolation.Expression, index))
            .ToArray();
    }

    /// <summary>
    /// Gets a parameter name from an interpolation expression.
    /// </summary>
    /// <param name="expression">The expression in the interpolation.</param>
    /// <param name="index">The parameter index.</param>
    /// <returns>The parameter name.</returns>
    private static string GetParameterName(ExpressionSyntax expression, int index)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => char.ToUpperInvariant(identifier.Identifier.ValueText[0]) +
                                             identifier.Identifier.ValueText.Substring(1),
            MemberAccessExpressionSyntax memberAccess => GetParameterName(memberAccess.Name, index),
            _ => $"Param{index}"
        };
    }

    /// <summary>
    /// Extracts a message template from a logging invocation.
    /// </summary>
    /// <param name="invocation">The logging invocation.</param>
    /// <returns>The message template.</returns>
    private static string ExtractMessageTemplate(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return string.Empty;

        var messageArgument = invocation.ArgumentList.Arguments[0];

        return messageArgument.Expression switch
        {
            InterpolatedStringExpressionSyntax interpolated =>
                ConvertInterpolationToTemplate(interpolated),
            _ => "TODO: Convert message to template"
        };
    }

    /// <summary>
    /// Converts an interpolated string to a message template.
    /// </summary>
    /// <param name="interpolated">The interpolated string.</param>
    /// <returns>The message template.</returns>
    private static string ConvertInterpolationToTemplate(InterpolatedStringExpressionSyntax interpolated)
    {
        var result = new System.Text.StringBuilder();
        var paramIndex = 0;

        foreach (var content in interpolated.Contents)
        {
            switch (content)
            {
                case InterpolatedStringTextSyntax text:
                    result.Append(text.TextToken.ValueText);
                    break;
                case InterpolationSyntax interpolation:
                    var paramName = GetParameterName(interpolation.Expression, paramIndex);
                    result.Append($"{{{paramName}}}");
                    paramIndex++;
                    break;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Checks if the logging call has an exception parameter.
    /// </summary>
    /// <param name="invocation">The logging invocation.</param>
    /// <returns>True if there's an exception parameter.</returns>
    private static bool HasExceptionParameter(InvocationExpressionSyntax invocation)
    {
        // Look for Exception-type arguments in the parameter list
        return invocation.ArgumentList.Arguments.Skip(1) // Skip the message argument
            .Any(arg => arg.ToString().ToLowerInvariant().Contains("exception"));
    }
}