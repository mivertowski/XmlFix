using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace XmlFix.Analyzers.Common;

/// <summary>
/// Utility class providing common syntax analysis helpers for analyzers.
/// </summary>
public static class SyntaxHelper
{
    /// <summary>
    /// Determines if a method declaration is a simple getter that could be a property.
    /// </summary>
    /// <param name="method">The method declaration to analyze.</param>
    /// <param name="semanticModel">The semantic model for the compilation.</param>
    /// <returns>True if the method is a candidate for property conversion.</returns>
    public static bool IsPropertyCandidate(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        // Must have no parameters
        if (method.ParameterList.Parameters.Count > 0)
            return false;

        // Must return a value (not void)
        if (method.ReturnType is PredefinedTypeSyntax predefinedType &&
            predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
            return false;

        // Must be public or protected
        var modifiers = method.Modifiers;
        if (!modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword)))
            return false;

        // Should not have expensive operation indicators (but allow property-like names)
        if (HasExpensiveOperationIndicators(method.Identifier.ValueText))
            return false;

        // Analyze the method body for complexity
        return IsSimpleMethodBody(method);
    }

    /// <summary>
    /// Checks if a method name suggests property-like behavior.
    /// </summary>
    /// <param name="methodName">The method name to check.</param>
    /// <returns>True if the name suggests property-like behavior.</returns>
    public static bool HasPropertyLikeName(string methodName)
    {
        return AnalyzerConstants.CommonPropertyMethodPrefixes
            .Any(prefix => methodName.StartsWith(prefix, StringComparison.Ordinal) &&
                          methodName.Length > prefix.Length);
    }

    /// <summary>
    /// Checks if a method name contains indicators of expensive operations.
    /// </summary>
    /// <param name="methodName">The method name to check.</param>
    /// <returns>True if the name suggests expensive operations.</returns>
    public static bool HasExpensiveOperationIndicators(string methodName)
    {
        // Methods with property-like prefixes are generally not expensive operations
        // They're typically simple queries or state checks
        // The actual expense should be determined by analyzing the method body
        if (HasPropertyLikeName(methodName))
        {
            return false;
        }

        // For non-property-like methods, check if they start with expensive operations
        return AnalyzerConstants.ExpensiveOperationIndicators
            .Any(indicator => methodName.StartsWith(indicator, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if a method body is simple enough to be converted to a property.
    /// </summary>
    /// <param name="method">The method declaration to analyze.</param>
    /// <returns>True if the method body is simple.</returns>
    public static bool IsSimpleMethodBody(MethodDeclarationSyntax method)
    {
        if (method.Body == null && method.ExpressionBody == null)
            return false;

        // Expression-bodied methods are generally good candidates
        if (method.ExpressionBody != null)
        {
            return IsSimpleExpression(method.ExpressionBody.Expression);
        }

        // For block bodies, check for simple patterns
        if (method.Body != null)
        {
            var statements = method.Body.Statements;

            // Should have exactly one statement
            if (statements.Count != 1)
                return false;

            // That statement should be a return statement
            if (statements[0] is ReturnStatementSyntax returnStatement)
            {
                return returnStatement.Expression != null &&
                       IsSimpleExpression(returnStatement.Expression);
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if an expression is simple enough for a property.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <returns>True if the expression is simple.</returns>
    public static bool IsSimpleExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            // Simple field access or property access
            MemberAccessExpressionSyntax memberAccess => IsSimpleMemberAccess(memberAccess),

            // Direct identifier (field/property)
            IdentifierNameSyntax => true,

            // Simple literals
            LiteralExpressionSyntax => true,

            // Simple conditional expressions (ternary operator with simple operands)
            ConditionalExpressionSyntax conditional =>
                IsSimpleExpression(conditional.Condition) &&
                IsSimpleExpression(conditional.WhenTrue) &&
                IsSimpleExpression(conditional.WhenFalse),

            // Simple null-coalescing expressions
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression) =>
                IsSimpleExpression(binary.Left) && IsSimpleExpression(binary.Right),

            // Simple cast expressions
            CastExpressionSyntax cast => IsSimpleExpression(cast.Expression),

            // Simple as expressions
            BinaryExpressionSyntax asExpression when asExpression.IsKind(SyntaxKind.AsExpression) =>
                IsSimpleExpression(asExpression.Left),

            _ => false
        };
    }

    /// <summary>
    /// Determines if a member access expression is simple (not a method call).
    /// </summary>
    /// <param name="memberAccess">The member access expression.</param>
    /// <returns>True if it's a simple member access.</returns>
    private static bool IsSimpleMemberAccess(MemberAccessExpressionSyntax memberAccess)
    {
        // Don't allow method calls
        if (memberAccess.Parent is InvocationExpressionSyntax)
            return false;

        // Allow simple property/field access on this or simple identifiers
        return memberAccess.Expression is ThisExpressionSyntax ||
               memberAccess.Expression is IdentifierNameSyntax;
    }

    /// <summary>
    /// Checks if an invocation expression is a string comparison method that needs StringComparison.
    /// </summary>
    /// <param name="invocation">The invocation expression to check.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <returns>True if this is a string method that should specify StringComparison.</returns>
    public static bool IsStringComparisonMethodCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var memberName = memberAccess.Name.Identifier.ValueText;
        if (!AnalyzerConstants.StringComparisonMethods.Contains(memberName))
            return false;

        // Check if the target is a string type
        var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
        if (typeInfo.Type?.SpecialType != SpecialType.System_String)
            return false;

        // Check if StringComparison parameter is already provided
        var arguments = invocation.ArgumentList.Arguments;
        return !arguments.Any(arg => IsStringComparisonArgument(arg, semanticModel));
    }

    /// <summary>
    /// Checks if an argument is a StringComparison parameter.
    /// </summary>
    /// <param name="argument">The argument to check.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <returns>True if the argument is a StringComparison.</returns>
    private static bool IsStringComparisonArgument(ArgumentSyntax argument, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(argument.Expression);
        return typeInfo.Type?.Name == "StringComparison" &&
               typeInfo.Type.ContainingNamespace?.ToDisplayString() == "System";
    }

    /// <summary>
    /// Checks if an invocation is a logger extension method call.
    /// </summary>
    /// <param name="invocation">The invocation expression to check.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <returns>True if this is a logger extension method call.</returns>
    public static bool IsLoggerExtensionMethodCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var memberName = memberAccess.Name.Identifier.ValueText;
        if (!AnalyzerConstants.LoggerExtensionMethods.Contains(memberName))
            return false;

        // Check if the target implements ILogger
        var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
        return IsLoggerType(typeInfo.Type);
    }

    /// <summary>
    /// Checks if a type is or implements ILogger.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is or implements ILogger.</returns>
    private static bool IsLoggerType(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        // Check direct type
        if (IsILoggerInterface(type))
            return true;

        // Check implemented interfaces
        return type.AllInterfaces.Any(IsILoggerInterface);
    }

    /// <summary>
    /// Checks if a type symbol represents the ILogger interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is ILogger.</returns>
    private static bool IsILoggerInterface(ITypeSymbol type)
    {
        return type.Name == "ILogger" &&
               type.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Logging";
    }

    /// <summary>
    /// Checks if a logging call uses string interpolation or concatenation that could benefit from LoggerMessage.
    /// </summary>
    /// <param name="invocation">The logger invocation to check.</param>
    /// <returns>True if the call uses string interpolation or concatenation.</returns>
    public static bool UsesStringInterpolationOrConcatenation(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return false;

        // Check the first argument (message argument)
        var messageArgument = invocation.ArgumentList.Arguments[0];
        return ContainsStringInterpolationOrConcatenation(messageArgument.Expression);
    }

    /// <summary>
    /// Recursively checks if an expression contains string interpolation or concatenation.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression contains interpolation or concatenation.</returns>
    private static bool ContainsStringInterpolationOrConcatenation(ExpressionSyntax expression)
    {
        return expression switch
        {
            InterpolatedStringExpressionSyntax => true,
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) => true,
            InvocationExpressionSyntax invocation => IsStringFormatCall(invocation),
            _ => false
        };
    }

    /// <summary>
    /// Checks if an invocation is a string.Format call.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <returns>True if this is a string.Format call.</returns>
    private static bool IsStringFormatCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.ValueText;
            if (methodName != "Format") return false;

            if (memberAccess.Expression is IdentifierNameSyntax identifier)
            {
                var typeName = identifier.Identifier.ValueText;
                return typeName == "string" || typeName == "String";
            }
        }

        return false;
    }
}