using Xunit;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Tests for the DocumentationGenerator utility class.
/// </summary>
public class DocumentationGeneratorTests
{
    /// <summary>
    /// Tests parameter description generation for common parameter names.
    /// </summary>
    [Theory]
    [InlineData("cancellationToken", "The cancellation token.")]
    [InlineData("id", "The identifier.")]
    [InlineData("userId", "The user identifier.")]
    [InlineData("userName", "The user name.")]
    [InlineData("isEnabled", "A value indicating whether enabled.")]
    [InlineData("canExecute", "A value indicating whether execute.")]
    [InlineData("hasPermission", "A value indicating whether permission.")]
    [InlineData("value", "The value.")]
    [InlineData("data", "The data.")]
    public void GenerateParameterDescription_CommonNames_ReturnsExpectedDescription(string parameterName, string expectedDescription)
    {
        var result = DocumentationGenerator.GenerateParameterDescription(parameterName);
        Assert.Equal(expectedDescription, result);
    }

    /// <summary>
    /// Tests parameter description generation for PascalCase names.
    /// </summary>
    [Theory]
    [InlineData("CustomerData", "The customer data.")]
    [InlineData("XMLDocument", "The X M L document.")]
    [InlineData("HTTPRequest", "The H T T P request.")]
    public void GenerateParameterDescription_PascalCaseNames_ParsesCorrectly(string parameterName, string expectedDescription)
    {
        var result = DocumentationGenerator.GenerateParameterDescription(parameterName);
        Assert.Equal(expectedDescription, result);
    }
}