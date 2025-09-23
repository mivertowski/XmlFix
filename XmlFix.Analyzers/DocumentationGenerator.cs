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
    private static readonly char[] SpaceSeparator = new[] { ' ' };
    /// <summary>
    /// Generates enhanced documentation with basic NLP improvements.
    /// </summary>
    /// <param name="symbol">The symbol to generate documentation for.</param>
    /// <param name="semanticModel">Optional semantic model for advanced analysis.</param>
    /// <returns>Enhanced documentation text.</returns>
    public static string GenerateEnhancedDocumentation(ISymbol symbol, SemanticModel? semanticModel = null)
    {
        // For now, use the intelligent summary with some basic enhancements
        var documentation = GenerateIntelligentSummary(symbol);

        // Apply basic NLP enhancements
        documentation = ApplyBasicEnhancements(documentation, symbol);

        return documentation;
    }

    /// <summary>
    /// Applies basic NLP enhancements to documentation.
    /// </summary>
    /// <param name="documentation">The base documentation.</param>
    /// <param name="symbol">The symbol being documented.</param>
    /// <returns>Enhanced documentation.</returns>
    private static string ApplyBasicEnhancements(string documentation, ISymbol symbol)
    {
        if (string.IsNullOrWhiteSpace(documentation))
            return documentation;

        var enhanced = documentation;

        // Domain-specific enhancements
        if (symbol.ContainingType != null)
        {
            var typeName = symbol.ContainingType.Name;

            // Web API enhancements
            if (typeName.EndsWith("Controller", StringComparison.Ordinal))
            {
                enhanced = EnhanceWebApiDocumentation(enhanced, symbol);
            }

            // Repository pattern enhancements
            if (typeName.EndsWith("Repository", StringComparison.Ordinal) || typeName.EndsWith("Service", StringComparison.Ordinal))
            {
                enhanced = EnhanceDataAccessDocumentation(enhanced, symbol);
            }
        }

        // Method-specific enhancements
        if (symbol is IMethodSymbol method)
        {
            enhanced = EnhanceMethodDocumentation(enhanced, method);
        }

        return enhanced;
    }

    private static string EnhanceWebApiDocumentation(string documentation, ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            // Check for HTTP attributes
            var httpAttributes = method.GetAttributes()
                .Where(attr => attr.AttributeClass?.Name.StartsWith("Http", StringComparison.Ordinal) == true)
                .ToList();

            if (httpAttributes.Count > 0)
            {
                var httpMethod = httpAttributes.First().AttributeClass?.Name.Substring(4);
                if (httpMethod != null && !documentation.Contains("HTTP") && !documentation.Contains("endpoint"))
                {
                    documentation = $"HTTP {httpMethod.ToUpperInvariant()} endpoint that {documentation.ToLowerInvariant()}";
                }
            }
        }

        return documentation;
    }

    private static string EnhanceDataAccessDocumentation(string documentation, ISymbol symbol)
    {
        if (symbol is IMethodSymbol method && method.IsAsync)
        {
            if (!documentation.Contains("async") && !documentation.Contains("Async"))
            {
                documentation = documentation.Replace("Gets", "Asynchronously retrieves");
                documentation = documentation.Replace("Creates", "Asynchronously creates");
                documentation = documentation.Replace("Updates", "Asynchronously updates");
                documentation = documentation.Replace("Deletes", "Asynchronously removes");
            }
        }

        return documentation;
    }

    private static string EnhanceMethodDocumentation(string documentation, IMethodSymbol method)
    {
        // Add async context
        if (method.IsAsync && !documentation.Contains("async"))
        {
            documentation += " This operation is performed asynchronously.";
        }

        // Add cancellation token context
        if (method.Parameters.Any(p => p.Type.Name.Contains("CancellationToken")))
        {
            documentation += " Supports cancellation via the provided cancellation token.";
        }

        // Add parameter context for common patterns
        if (method.Parameters.Any(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase)))
        {
            if (method.ContainingType?.Name.EndsWith("Controller", StringComparison.Ordinal) == true)
            {
                documentation += " Uses the ID parameter from the route.";
            }
        }

        return documentation;
    }

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
        var typeWord = GetTypeVerb(typeKind);

        var parsedName = ParsePascalCase(name);

        // For better readability, use "represents" for classes
        if (typeKind == TypeKind.Class)
        {
            return $"{article} {typeWord} that represents {parsedName.ToLowerInvariant()}.";
        }

        return $"{article} {parsedName.ToLowerInvariant()} {typeWord}.";
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
            // Check if this is a boolean method to include return info in summary
            if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean)
            {
                return $"Returns true if able to {action.ToLowerInvariant()}, otherwise false.";
            }
            return $"Attempts to {action.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Validate", StringComparison.Ordinal))
        {
            var objectName = ParsePascalCase(name.Substring(8));
            return $"Validates the {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Initialize", StringComparison.Ordinal))
        {
            var objectName = ParsePascalCase(name.Substring(10));
            return $"Initializes the {objectName.ToLowerInvariant()}.";
        }

        if (name.StartsWith("Process", StringComparison.Ordinal))
        {
            var objectName = ParsePascalCase(name.Substring(7));
            return $"Processes the {objectName.ToLowerInvariant()}.";
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
            "op_Addition" => "Implements the addition operator to add two values.",
            "op_Subtraction" => "Implements the subtraction operator to subtract one value from another.",
            "op_Multiply" => "Implements the multiplication operator to multiply two values.",
            "op_Division" => "Implements the division operator to divide one value by another.",
            "op_Modulus" => "Implements the modulus operator to get the remainder of division.",
            "op_Equality" => "Implements the equality operator to determine whether two values are equal.",
            "op_Inequality" => "Implements the inequality operator to determine whether two values are not equal.",
            "op_GreaterThan" => "Determines whether one value is greater than another.",
            "op_LessThan" => "Determines whether one value is less than another.",
            "op_GreaterThanOrEqual" => "Determines whether one value is greater than or equal to another.",
            "op_LessThanOrEqual" => "Determines whether one value is less than or equal to another.",
            "op_BitwiseAnd" => "Implements the bitwise AND operator.",
            "op_BitwiseOr" => "Implements the bitwise OR operator.",
            "op_ExclusiveOr" => "Implements the bitwise XOR operator.",
            "op_LeftShift" => "Implements the left shift operator.",
            "op_RightShift" => "Implements the right shift operator.",
            "op_LogicalNot" => "Implements the logical negation operator.",
            "op_OnesComplement" => "Implements the bitwise complement operator.",
            "op_Increment" => "Implements the increment operator.",
            "op_Decrement" => "Implements the decrement operator.",
            "op_True" => "Implements the true operator.",
            "op_False" => "Implements the false operator.",
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
        // Handle boolean-style properties
        if (propertyName.StartsWith("Is", StringComparison.Ordinal) ||
            propertyName.StartsWith("Has", StringComparison.Ordinal) ||
            propertyName.StartsWith("Can", StringComparison.Ordinal))
        {
            var prefixLength = propertyName.StartsWith("Is", StringComparison.Ordinal) ? 2 :
                              propertyName.StartsWith("Has", StringComparison.Ordinal) ? 3 : 3;
            var remainder = propertyName.Substring(prefixLength);
            if (remainder.Length > 0)
            {
                var parsedRemainder = ParsePascalCase(remainder);
                return $"Gets or sets a value indicating whether {parsedRemainder.ToLowerInvariant()}.";
            }
        }

        // Handle Id suffix - expand to identifier
        if (propertyName.EndsWith("Id", StringComparison.Ordinal) && propertyName.Length > 2)
        {
            var prefix = propertyName.Substring(0, propertyName.Length - 2);
            var parsedPrefix = ParsePascalCase(prefix);
            return $"Gets or sets the {parsedPrefix.ToLowerInvariant()} identifier.";
        }

        // Default case
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

        // Handle special single character and common parameters
        if (parameterName.Equals("e", StringComparison.OrdinalIgnoreCase))
        {
            return "The event args.";
        }

        if (parameterName.Equals("args", StringComparison.OrdinalIgnoreCase))
        {
            return "The arguments.";
        }

        if (parameterName.Equals("x", StringComparison.OrdinalIgnoreCase))
        {
            return "The x coordinate.";
        }

        if (parameterName.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            return "The y coordinate.";
        }

        if (parameterName.Equals("z", StringComparison.OrdinalIgnoreCase))
        {
            return "The z coordinate.";
        }

        if (parameterName.Equals("sender", StringComparison.OrdinalIgnoreCase))
        {
            return "The sender.";
        }

        if (parameterName.Equals("index", StringComparison.OrdinalIgnoreCase))
        {
            return "The index.";
        }

        if (parameterName.Equals("count", StringComparison.OrdinalIgnoreCase))
        {
            return "The count.";
        }

        if (parameterName.Equals("length", StringComparison.OrdinalIgnoreCase))
        {
            return "The length.";
        }

        if (parameterName.Equals("width", StringComparison.OrdinalIgnoreCase))
        {
            return "The width.";
        }

        if (parameterName.Equals("height", StringComparison.OrdinalIgnoreCase))
        {
            return "The height.";
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
            var prefixLength = parameterName.StartsWith("is", StringComparison.OrdinalIgnoreCase) ? 2 : 3;
            var remainder = parameterName.Substring(prefixLength);
            if (remainder.Length > 0)
            {
                remainder = char.ToLowerInvariant(remainder[0]) + remainder.Substring(1);
            }
            return $"A value indicating whether {remainder}.";
        }

        var parsedName = ParsePascalCase(parameterName);
        // Preserve case for acronyms and single letters
        var words = parsedName.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 1 && !words[i].All(char.IsUpper))
            {
                words[i] = words[i].ToLowerInvariant();
            }
        }
        return $"The {string.Join(" ", words)}.";
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

        // Handle consecutive uppercase letters (acronyms) by adding spaces between each
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < pascalCaseName.Length; i++)
        {
            char current = pascalCaseName[i];

            // Add space before uppercase letters (except the first one)
            if (i > 0 && char.IsUpper(current))
            {
                // Check if it's part of an acronym
                bool isAcronym = i > 0 && char.IsUpper(pascalCaseName[i - 1]);
                bool nextIsLower = i < pascalCaseName.Length - 1 && char.IsLower(pascalCaseName[i + 1]);

                // Add space in all cases
                result.Append(' ');
            }

            result.Append(current);
        }

        return result.ToString();
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