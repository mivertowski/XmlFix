using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlFix.Analyzers;

/// <summary>
/// Utility class for generating intelligent XML documentation content.
/// </summary>
public static class DocumentationGenerator
{
    /// <summary>
    /// Generates an intelligent summary based on the symbol name and type.
    /// </summary>
    /// <param name="symbol">The symbol to generate a summary for.</param>
    /// <returns>An intelligent summary text.</returns>
    public static string GenerateIntelligentSummary(ISymbol symbol)
    {
        var name = symbol.Name;

        return symbol.Kind switch
        {
            SymbolKind.NamedType => GenerateTypeSummary((INamedTypeSymbol)symbol),
            SymbolKind.Method => GenerateMethodSummary((IMethodSymbol)symbol),
            SymbolKind.Property => GeneratePropertySummary(name),
            SymbolKind.Field => GenerateFieldSummary(name),
            SymbolKind.Event => GenerateEventSummary(name),
            _ => "TODO: Add description."
        };
    }

    /// <summary>
    /// Generates a summary for a type symbol.
    /// </summary>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <returns>A summary for the type.</returns>
    private static string GenerateTypeSummary(INamedTypeSymbol typeSymbol)
    {
        var name = typeSymbol.Name;
        var typeKind = typeSymbol.TypeKind;

        var article = GetArticle(typeKind);
        var verb = GetTypeVerb(typeKind);

        var parsedName = ParsePascalCase(name);
        return $"{article} {parsedName.ToLowerInvariant()} {verb}.";
    }

    /// <summary>
    /// Generates a summary for a method symbol.
    /// </summary>
    /// <param name="methodSymbol">The method symbol.</param>
    /// <returns>A summary for the method.</returns>
    private static string GenerateMethodSummary(IMethodSymbol methodSymbol)
    {
        var name = methodSymbol.Name;

        return methodSymbol.MethodKind switch
        {
            MethodKind.Constructor => $"Initializes a new instance of the {methodSymbol.ContainingType.Name} class.",
            MethodKind.Destructor => $"Finalizes an instance of the {methodSymbol.ContainingType.Name} class.",
            MethodKind.UserDefinedOperator => GenerateOperatorSummary(name),
            _ => GenerateRegularMethodSummary(name, methodSymbol)
        };
    }

    /// <summary>
    /// Generates a summary for a regular method.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="methodSymbol">The method symbol.</param>
    /// <returns>A summary for the method.</returns>
    private static string GenerateRegularMethodSummary(string name, IMethodSymbol methodSymbol)
    {
        // Handle common method patterns
        if (name.StartsWith("Get", StringComparison.Ordinal))
        {
            var objectName = ParsePascalCase(name.Substring(3));
            return $"Gets the {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Set", StringComparison.Ordinal))
        {
            var objectName = ParsePascalCase(name.Substring(3));
            return $"Sets the {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Create", StringComparison.Ordinal))
        {
            var objectName = ParsePascalCase(name.Substring(6));
            return $"Creates a new {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Delete", StringComparison.Ordinal) || name.StartsWith("Remove", StringComparison.Ordinal))
        {
            var prefix = name.StartsWith("Delete", StringComparison.Ordinal) ? "Delete" : "Remove";
            var objectName = ParsePascalCase(name.Substring(prefix.Length));
            return $"Deletes the {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Update", StringComparison.Ordinal))
        {
            var objectName = ParsePascalCase(name.Substring(6));
            return $"Updates the {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Find", StringComparison.Ordinal))
        {
            var objectName = ParsePascalCase(name.Substring(4));
            return $"Finds the {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Calculate", StringComparison.Ordinal) || name.StartsWith("Compute", StringComparison.Ordinal))
        {
            var prefix = name.StartsWith("Calculate", StringComparison.Ordinal) ? "Calculate" : "Compute";
            var objectName = ParsePascalCase(name.Substring(prefix.Length));
            return $"Calculates the {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Is", StringComparison.Ordinal) || name.StartsWith("Has", StringComparison.Ordinal) || name.StartsWith("Can", StringComparison.Ordinal))
        {
            var remainder = name.StartsWith("Is", StringComparison.Ordinal) ? name.Substring(2) :
                           name.StartsWith("Has", StringComparison.Ordinal) ? name.Substring(3) : name.Substring(3);
            var description = ParsePascalCase(remainder);
            var questionVerb = name.StartsWith("Is", StringComparison.Ordinal) ? "whether" :
                      name.StartsWith("Has", StringComparison.Ordinal) ? "whether" : "whether";
            return $"Determines {questionVerb} {description.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Try", StringComparison.Ordinal))
        {
            var action = ParsePascalCase(name.Substring(3));
            return $"Attempts to {action.ToLowerInvariant()}.";
        }

        // Check if it's async
        if (name.EndsWith("Async", StringComparison.Ordinal))
        {
            var baseName = name.Substring(0, name.Length - 5);
            var baseDescription = GenerateRegularMethodSummary(baseName, methodSymbol);
            return baseDescription.Replace(".", " asynchronously.");
        }

        // Default case - use the method name
        var parsedName = ParsePascalCase(name);
        var verb = GetMethodVerb(methodSymbol);
        return $"{verb} {parsedName.ToLowerInvariant()}.";
    }

    /// <summary>
    /// Generates a summary for an operator method.
    /// </summary>
    /// <param name="operatorName">The operator name.</param>
    /// <returns>A summary for the operator.</returns>
    private static string GenerateOperatorSummary(string operatorName)
    {
        return operatorName switch
        {
            "op_Addition" => "Adds two values.",
            "op_Subtraction" => "Subtracts one value from another.",
            "op_Multiply" => "Multiplies two values.",
            "op_Division" => "Divides one value by another.",
            "op_Equality" => "Determines whether two values are equal.",
            "op_Inequality" => "Determines whether two values are not equal.",
            "op_GreaterThan" => "Determines whether one value is greater than another.",
            "op_LessThan" => "Determines whether one value is less than another.",
            "op_GreaterThanOrEqual" => "Determines whether one value is greater than or equal to another.",
            "op_LessThanOrEqual" => "Determines whether one value is less than or equal to another.",
            "op_Implicit" => "Defines an implicit conversion operator.",
            "op_Explicit" => "Defines an explicit conversion operator.",
            _ => "Defines a custom operator."
        };
    }

    /// <summary>
    /// Generates a summary for a property.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns>A summary for the property.</returns>
    private static string GeneratePropertySummary(string propertyName)
    {
        var parsedName = ParsePascalCase(propertyName);
        return $"Gets or sets the {parsedName.ToLowerInvariant()}.";
    }

    /// <summary>
    /// Generates a summary for a field.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <returns>A summary for the field.</returns>
    private static string GenerateFieldSummary(string fieldName)
    {
        var parsedName = ParsePascalCase(fieldName);
        return $"The {parsedName.ToLowerInvariant()}.";
    }

    /// <summary>
    /// Generates a summary for an event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <returns>A summary for the event.</returns>
    private static string GenerateEventSummary(string eventName)
    {
        var parsedName = ParsePascalCase(eventName);
        return $"Occurs when {parsedName.ToLowerInvariant()}.";
    }

    /// <summary>
    /// Generates a parameter description based on the parameter name.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <returns>A description for the parameter.</returns>
    public static string GenerateParameterDescription(string parameterName)
    {
        // Handle common parameter patterns
        if (parameterName.Equals("cancellationToken", StringComparison.OrdinalIgnoreCase))
        {
            return "The cancellation token.";
        }

        if (parameterName.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            return "The identifier.";
        }

        if (parameterName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = parameterName.Substring(0, parameterName.Length - 2);
            var parsedPrefix = ParsePascalCase(prefix);
            return $"The {parsedPrefix.ToLowerInvariant()} identifier.";
        }

        if (parameterName.StartsWith("is", StringComparison.OrdinalIgnoreCase) ||
            parameterName.StartsWith("can", StringComparison.OrdinalIgnoreCase) ||
            parameterName.StartsWith("has", StringComparison.OrdinalIgnoreCase))
        {
            var remainder = parameterName.Substring(2);
            if (remainder.Length > 0)
            {
                remainder = char.ToLowerInvariant(remainder[0]) + remainder.Substring(1);
            }
            var verb = parameterName.StartsWith("is", StringComparison.OrdinalIgnoreCase) ? "whether" :
                      parameterName.StartsWith("can", StringComparison.OrdinalIgnoreCase) ? "whether" : "whether";
            return $"A value indicating {verb} {ParsePascalCase(remainder).ToLowerInvariant()}.";
        }

        var parsedName = ParsePascalCase(parameterName);
        return $"The {parsedName.ToLowerInvariant()}.";
    }

    /// <summary>
    /// Generates a return description based on the symbol.
    /// </summary>
    /// <param name="symbol">The symbol to generate a return description for.</param>
    /// <returns>A description for the return value.</returns>
    public static string GenerateReturnDescription(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            var name = method.Name;

            if (name.StartsWith("Is", StringComparison.Ordinal) ||
                name.StartsWith("Has", StringComparison.Ordinal) ||
                name.StartsWith("Can", StringComparison.Ordinal))
            {
                return "true if the condition is met; otherwise, false.";
            }

            if (name.StartsWith("Get", StringComparison.Ordinal))
            {
                var objectName = ParsePascalCase(name.Substring(3));
                return $"The {objectName.ToLowerInvariant()}.";
            }

            if (name.StartsWith("Create", StringComparison.Ordinal))
            {
                var objectName = ParsePascalCase(name.Substring(6));
                return $"The created {objectName.ToLowerInvariant()}.";
            }

            if (name.StartsWith("Find", StringComparison.Ordinal))
            {
                var objectName = ParsePascalCase(name.Substring(4));
                return $"The found {objectName.ToLowerInvariant()}, or null if not found.";
            }

            if (name.StartsWith("Calculate", StringComparison.Ordinal) || name.StartsWith("Compute", StringComparison.Ordinal))
            {
                var prefix = name.StartsWith("Calculate", StringComparison.Ordinal) ? "Calculate" : "Compute";
                var objectName = ParsePascalCase(name.Substring(prefix.Length));
                return $"The calculated {objectName.ToLowerInvariant()}.";
            }

            if (name.StartsWith("Try", StringComparison.Ordinal))
            {
                return "true if the operation succeeded; otherwise, false.";
            }
        }

        return "The result of the operation.";
    }

    /// <summary>
    /// Generates a value description for properties.
    /// </summary>
    /// <param name="symbol">The property symbol.</param>
    /// <returns>A description for the property value.</returns>
    public static string GenerateValueDescription(ISymbol symbol)
    {
        var parsedName = ParsePascalCase(symbol.Name);
        return $"The {parsedName.ToLowerInvariant()}.";
    }

    /// <summary>
    /// Parses PascalCase names into readable phrases.
    /// </summary>
    /// <param name="pascalCaseName">The PascalCase name to parse.</param>
    /// <returns>A readable phrase.</returns>
    private static string ParsePascalCase(string pascalCaseName)
    {
        if (string.IsNullOrEmpty(pascalCaseName))
            return pascalCaseName;

        // Insert spaces before uppercase letters (except the first one)
        var result = Regex.Replace(pascalCaseName, @"(?<!^)(?=[A-Z])", " ");

        // Handle acronyms properly (e.g., XMLDocument -> XML Document)
        result = Regex.Replace(result, @"([A-Z]+)([A-Z][a-z])", "$1 $2");

        return result;
    }

    /// <summary>
    /// Gets the appropriate article for a type kind.
    /// </summary>
    /// <param name="typeKind">The type kind.</param>
    /// <returns>The appropriate article ("A", "An", or "The").</returns>
    private static string GetArticle(TypeKind typeKind)
    {
        return typeKind switch
        {
            TypeKind.Interface => "An",
            TypeKind.Enum => "An",
            _ => "A"
        };
    }

    /// <summary>
    /// Gets the appropriate verb for a type kind.
    /// </summary>
    /// <param name="typeKind">The type kind.</param>
    /// <returns>The appropriate verb.</returns>
    private static string GetTypeVerb(TypeKind typeKind)
    {
        return typeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Interface => "interface",
            TypeKind.Struct => "structure",
            TypeKind.Enum => "enumeration",
            TypeKind.Delegate => "delegate",
            _ => "type"
        };
    }

    /// <summary>
    /// Gets the appropriate verb for a method.
    /// </summary>
    /// <param name="methodSymbol">The method symbol.</param>
    /// <returns>The appropriate verb.</returns>
    private static string GetMethodVerb(IMethodSymbol methodSymbol)
    {
        // Check return type to determine if it's a query or command
        if (methodSymbol.ReturnsVoid)
        {
            return "Performs";
        }

        if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean)
        {
            return "Determines";
        }

        return "Gets";
    }
}