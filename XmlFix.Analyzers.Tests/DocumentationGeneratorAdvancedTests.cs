using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using XmlFix.Analyzers;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Advanced tests for DocumentationGenerator.
/// </summary>
public class DocumentationGeneratorAdvancedTests
{
    /// <summary>
    /// Tests intelligent summary generation for types.
    /// </summary>
    [Theory]
    [InlineData("UserService", "user service")]
    [InlineData("IRepository", "repository")]
    [InlineData("CustomerManager", "customer manager")]
    [InlineData("DataProcessor", "data processor")]
    public async Task GenerateIntelligentSummary_ForTypes(string typeName, string expectedPart)
    {
        var code = $@"
namespace Test
{{
    public class {typeName} {{ }}
}}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var classDecl = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(classDecl);

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains(expectedPart, summary.ToLowerInvariant());
    }

    /// <summary>
    /// Tests intelligent summary generation for methods.
    /// </summary>
    [Theory]
    [InlineData("GetUser", "Gets the user")]
    [InlineData("SetValue", "Sets the value")]
    [InlineData("CreateInstance", "Creates a new instance")]
    [InlineData("DeleteRecord", "Deletes the record")]
    [InlineData("UpdateSettings", "Updates the settings")]
    [InlineData("FindItems", "Finds the items")]
    [InlineData("CalculateTotal", "Calculates the total")]
    [InlineData("ValidateInput", "Validates the input")]
    [InlineData("ProcessData", "Processes the data")]
    [InlineData("InitializeComponent", "Initializes the component")]
    [InlineData("IsValid", "Determines whether valid")]
    [InlineData("HasPermission", "Determines whether permission")]
    [InlineData("CanExecute", "Determines whether execute")]
    public async Task GenerateIntelligentSummary_ForMethods(string methodName, string expectedSummary)
    {
        var code = $@"
namespace Test
{{
    public class TestClass
    {{
        public void {methodName}() {{ }}
    }}
}}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var methodDecl = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(methodDecl);

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains(expectedSummary, summary);
    }

    /// <summary>
    /// Tests intelligent summary for constructors.
    /// </summary>
    [Fact]
    public async Task GenerateIntelligentSummary_ForConstructor()
    {
        var code = @"
namespace Test
{
    public class MyClass
    {
        public MyClass() { }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var ctorDecl = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(ctorDecl);

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains("Initializes a new instance", summary);
        Assert.Contains("MyClass", summary);
    }

    /// <summary>
    /// Tests intelligent summary for properties.
    /// </summary>
    [Theory]
    [InlineData("Name", "Gets or sets the name")]
    [InlineData("IsActive", "Gets or sets a value indicating whether active")]
    [InlineData("Count", "Gets or sets the count")]
    [InlineData("UserId", "Gets or sets the user identifier")]
    public async Task GenerateIntelligentSummary_ForProperties(string propertyName, string expectedSummary)
    {
        var code = $@"
namespace Test
{{
    public class TestClass
    {{
        public string {propertyName} {{ get; set; }}
    }}
}}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var propDecl = tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(propDecl);

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains(expectedSummary, summary);
    }

    /// <summary>
    /// Tests intelligent summary for events.
    /// </summary>
    [Theory]
    [InlineData("DataChanged", "occurs when data changed")]
    [InlineData("Clicked", "occurs when clicked")]
    [InlineData("UserLoggedIn", "occurs when user logged in")]
    public async Task GenerateIntelligentSummary_ForEvents(string eventName, string expectedPart)
    {
        var code = $@"
namespace Test
{{
    public class TestClass
    {{
        public event EventHandler {eventName};
    }}
}}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var eventDecl = tree.GetRoot().DescendantNodes().OfType<EventFieldDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(eventDecl.Declaration.Variables.First());

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains(expectedPart, summary.ToLowerInvariant());
    }

    /// <summary>
    /// Tests intelligent summary for fields.
    /// </summary>
    [Theory]
    [InlineData("MaxValue", "max value")]
    [InlineData("DefaultTimeout", "default timeout")]
    [InlineData("_instance", "instance")]
    public async Task GenerateIntelligentSummary_ForFields(string fieldName, string expectedPart)
    {
        var code = $@"
namespace Test
{{
    public class TestClass
    {{
        public const int {fieldName} = 100;
    }}
}}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var fieldDecl = tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(fieldDecl.Declaration.Variables.First());

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains(expectedPart, summary.ToLowerInvariant());
    }

    /// <summary>
    /// Tests intelligent summary for interfaces.
    /// </summary>
    [Fact]
    public async Task GenerateIntelligentSummary_ForInterface()
    {
        var code = @"
namespace Test
{
    public interface IService { }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var interfaceDecl = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(interfaceDecl);

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains("interface", summary);
    }

    /// <summary>
    /// Tests intelligent summary for enums.
    /// </summary>
    [Fact]
    public async Task GenerateIntelligentSummary_ForEnum()
    {
        var code = @"
namespace Test
{
    public enum Status { }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var enumDecl = tree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(enumDecl);

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains("enumeration", summary);
    }

    /// <summary>
    /// Tests intelligent summary for delegates.
    /// </summary>
    [Fact]
    public async Task GenerateIntelligentSummary_ForDelegate()
    {
        var code = @"
namespace Test
{
    public delegate void EventHandler();
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var delegateDecl = tree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(delegateDecl);

        var summary = DocumentationGenerator.GenerateIntelligentSummary(symbol);

        Assert.NotNull(summary);
        Assert.Contains("delegate", summary);
    }

    /// <summary>
    /// Tests parameter description generation for special cases.
    /// </summary>
    [Theory]
    [InlineData("e", "The event args.")]
    [InlineData("sender", "The sender.")]
    [InlineData("args", "The arguments.")]
    [InlineData("index", "The index.")]
    [InlineData("count", "The count.")]
    [InlineData("length", "The length.")]
    [InlineData("width", "The width.")]
    [InlineData("height", "The height.")]
    [InlineData("x", "The x coordinate.")]
    [InlineData("y", "The y coordinate.")]
    [InlineData("z", "The z coordinate.")]
    public void GenerateParameterDescription_SpecialCases(string paramName, string expected)
    {
        var result = DocumentationGenerator.GenerateParameterDescription(paramName);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests return description generation.
    /// </summary>
    [Fact]
    public async Task GenerateReturnDescription_ForBooleanMethod()
    {
        var code = @"
namespace Test
{
    public class TestClass
    {
        public bool IsValid() { return true; }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var methodDecl = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(methodDecl);

        var returnDesc = DocumentationGenerator.GenerateReturnDescription(symbol);

        Assert.NotNull(returnDesc);
        Assert.Contains("true if", returnDesc);
        Assert.Contains("false", returnDesc);
    }

    /// <summary>
    /// Tests return description for Try methods.
    /// </summary>
    [Fact]
    public async Task GenerateReturnDescription_ForTryMethod()
    {
        var code = @"
namespace Test
{
    public class TestClass
    {
        public bool TryParse(string input) { return true; }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var methodDecl = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(methodDecl);

        var returnDesc = DocumentationGenerator.GenerateReturnDescription(symbol);

        Assert.NotNull(returnDesc);
        Assert.Contains("true if the operation succeeded", returnDesc);
    }

    /// <summary>
    /// Tests value description generation.
    /// </summary>
    [Fact]
    public async Task GenerateValueDescription_ForProperty()
    {
        var code = @"
namespace Test
{
    public class TestClass
    {
        public string UserName { get; set; }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var model = compilation.GetSemanticModel(tree);
        var propDecl = tree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        var symbol = model.GetDeclaredSymbol(propDecl);

        var valueDesc = DocumentationGenerator.GenerateValueDescription(symbol);

        Assert.NotNull(valueDesc);
        Assert.Contains("user name", valueDesc.ToLowerInvariant());
    }
}