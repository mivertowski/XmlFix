using System;
using Xunit;
using XmlFix.Analyzers;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Basic unit tests for the analyzer components.
/// </summary>
public class BasicTests
{
    /// <summary>
    /// Tests that the analyzer can be instantiated.
    /// </summary>
    [Fact]
    public void AnalyzerCanBeInstantiated()
    {
        var analyzer = new MissingXmlDocsAnalyzer();
        Assert.NotNull(analyzer);
        Assert.NotEmpty(analyzer.SupportedDiagnostics);
    }

    /// <summary>
    /// Tests that the code fix provider can be instantiated.
    /// </summary>
    [Fact]
    public void CodeFixProviderCanBeInstantiated()
    {
        var codeFix = new MissingXmlDocsCodeFix();
        Assert.NotNull(codeFix);
        Assert.Contains("XDOC001", codeFix.FixableDiagnosticIds);
    }

    /// <summary>
    /// Tests documentation generator for common patterns.
    /// </summary>
    [Theory]
    [InlineData("cancellationToken", "The cancellation token.")]
    [InlineData("id", "The identifier.")]
    [InlineData("userId", "The user identifier.")]
    [InlineData("value", "The value.")]
    public void DocumentationGenerator_GeneratesCorrectParameterDescriptions(string parameterName, string expected)
    {
        var result = DocumentationGenerator.GenerateParameterDescription(parameterName);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that the diagnostic ID is correct.
    /// </summary>
    [Fact]
    public void DiagnosticIdIsCorrect()
    {
        Assert.Equal("XDOC001", MissingXmlDocsAnalyzer.MissingXmlDocsId);
    }

    /// <summary>
    /// Tests the diagnostic descriptor properties.
    /// </summary>
    [Fact]
    public void DiagnosticDescriptorIsConfiguredCorrectly()
    {
        var analyzer = new MissingXmlDocsAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        Assert.Equal("XDOC001", descriptor.Id);
        Assert.Equal("Documentation", descriptor.Category);
        Assert.True(descriptor.IsEnabledByDefault);
    }
}