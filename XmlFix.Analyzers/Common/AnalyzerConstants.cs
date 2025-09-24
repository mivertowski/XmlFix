using Microsoft.CodeAnalysis;

namespace XmlFix.Analyzers.Common;

/// <summary>
/// Shared constants for all analyzers in the XmlFix project.
/// </summary>
public static class AnalyzerConstants
{
    /// <summary>
    /// Diagnostic ID for CA1024: Use properties where appropriate.
    /// </summary>
    public const string UsePropertiesWhereAppropriateId = "XFIX001";

    /// <summary>
    /// Diagnostic ID for CA1310: Specify StringComparison.
    /// </summary>
    public const string SpecifyStringComparisonId = "XFIX002";

    /// <summary>
    /// Diagnostic ID for CA1848: Use LoggerMessage delegates.
    /// </summary>
    public const string UseLoggerMessageDelegatesId = "XFIX003";

    /// <summary>
    /// Category for performance-related diagnostics.
    /// </summary>
    public const string PerformanceCategory = "Performance";

    /// <summary>
    /// Category for design-related diagnostics.
    /// </summary>
    public const string DesignCategory = "Design";

    /// <summary>
    /// Category for usage-related diagnostics.
    /// </summary>
    public const string UsageCategory = "Usage";

    /// <summary>
    /// Default diagnostic severity for code quality improvements.
    /// </summary>
    public const DiagnosticSeverity DefaultSeverity = DiagnosticSeverity.Warning;

    /// <summary>
    /// Help link base URL for Microsoft documentation.
    /// </summary>
    public const string MicrosoftDocsBaseUrl = "https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/";

    /// <summary>
    /// Common method names that typically should be properties.
    /// </summary>
    public static readonly string[] CommonPropertyMethodPrefixes = new[]
    {
        "Get",
        "Is",
        "Has",
        "Can"
    };

    /// <summary>
    /// String comparison method names that have StringComparison parameter overloads.
    /// </summary>
    public static readonly string[] StringComparisonMethods = new[]
    {
        "StartsWith",
        "EndsWith",
        "Equals",
        "IndexOf",
        "LastIndexOf",
        "Contains", // Available in .NET Core 2.1+ and .NET 8.0
        "Compare" // Static string.Compare method with StringComparison overload
        // Note: CompareTo does NOT have StringComparison overload
    };

    /// <summary>
    /// ILogger extension method names that should use LoggerMessage pattern.
    /// </summary>
    public static readonly string[] LoggerExtensionMethods = new[]
    {
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical"
    };

    /// <summary>
    /// Common expensive operation indicators in method names.
    /// </summary>
    public static readonly string[] ExpensiveOperationIndicators = new[]
    {
        "Calculate",
        "Compute",
        "Process",
        "Load",
        "Save",
        "Read",
        "Write",
        "Execute",
        "Invoke",
        "Call",
        "Send",
        "Receive",
        "Download",
        "Upload",
        "Query",
        "Search",
        "Find",
        "Connect",
        "Disconnect",
        "Initialize",
        "Dispose"
    };
}