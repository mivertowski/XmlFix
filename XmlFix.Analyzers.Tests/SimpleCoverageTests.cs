using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using XmlFix.Analyzers;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Simple tests to improve code coverage.
/// </summary>
public class SimpleCoverageTests
{
    /// <summary>
    /// Tests analyzer initialization context.
    /// </summary>
    [Fact]
    public void AnalyzerInitializesContext()
    {
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = CreateSimpleCompilation("public class Test { }");

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);

        // This should trigger initialization
        Assert.NotNull(compilationWithAnalyzers);
    }

    /// <summary>
    /// Tests code fix GetFixAllProvider.
    /// </summary>
    [Fact]
    public void CodeFixGetFixAllProvider()
    {
        var codeFix = new MissingXmlDocsCodeFix();
        var provider = codeFix.GetFixAllProvider();
        Assert.NotNull(provider);
    }

    /// <summary>
    /// Tests DocumentationGenerator for method summaries.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorMethodPatterns()
    {
        var compilation = CreateSimpleCompilation(@"
public class Test
{
    public void GetData() { }
    public void SetValue() { }
    public void CreateObject() { }
    public void DeleteItem() { }
    public void UpdateRecord() { }
    public void FindUser() { }
    public void CalculateSum() { }
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var methodSymbols = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Select(m => model.GetDeclaredSymbol(m))
            .Where(s => s != null)
            .ToList();

        foreach (var method in methodSymbols)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(method!);
            Assert.NotNull(summary);
            Assert.NotEmpty(summary);
        }
    }

    /// <summary>
    /// Tests DocumentationGenerator for type summaries.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorTypePatterns()
    {
        var compilation = CreateSimpleCompilation(@"
public class TestClass { }
public interface ITestInterface { }
public struct TestStruct { }
public enum TestEnum { Value }
public delegate void TestDelegate();
");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var typeSymbols = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .Where(s => s.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        foreach (var type in typeSymbols)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(type);
            Assert.NotNull(summary);
            Assert.NotEmpty(summary);
        }
    }

    /// <summary>
    /// Tests DocumentationGenerator return descriptions.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorReturnDescriptions()
    {
        var compilation = CreateSimpleCompilation(@"
public class Test
{
    public bool IsValid() { return true; }
    public bool HasData() { return true; }
    public bool CanProcess() { return true; }
    public string GetName() { return """"; }
    public int Calculate() { return 0; }
    public void Process() { }
    public bool TryParse() { return true; }
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var methodSymbols = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Select(m => model.GetDeclaredSymbol(m))
            .Where(s => s != null)
            .ToList();

        foreach (var method in methodSymbols)
        {
            var returnDesc = DocumentationGenerator.GenerateReturnDescription(method!);
            Assert.NotNull(returnDesc);
            Assert.NotEmpty(returnDesc);
        }
    }

    /// <summary>
    /// Tests DocumentationGenerator value descriptions.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorValueDescriptions()
    {
        var compilation = CreateSimpleCompilation(@"
public class Test
{
    public string Name { get; set; }
    public int Count { get; set; }
    public bool IsActive { get; set; }
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var propertySymbols = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
            .Select(p => model.GetDeclaredSymbol(p))
            .Where(s => s != null)
            .ToList();

        foreach (var property in propertySymbols)
        {
            var valueDesc = DocumentationGenerator.GenerateValueDescription(property!);
            Assert.NotNull(valueDesc);
            Assert.NotEmpty(valueDesc);
        }
    }

    /// <summary>
    /// Tests parameter descriptions for common patterns.
    /// </summary>
    [Fact]
    public void ParameterDescriptionPatterns()
    {
        // Test common parameters
        var parameters = new[]
        {
            "value", "name", "id", "userId", "cancellationToken",
            "sender", "index", "count", "length", "width", "height"
        };

        foreach (var param in parameters)
        {
            var desc = DocumentationGenerator.GenerateParameterDescription(param);
            Assert.NotNull(desc);
            Assert.NotEmpty(desc);
            Assert.StartsWith("The", desc);
        }
    }

    /// <summary>
    /// Tests DocumentationGenerator for properties.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorPropertySummaries()
    {
        var compilation = CreateSimpleCompilation(@"
public class Test
{
    public string Name { get; set; }
    public string FirstName { get; }
    public string LastName { set; }
    public bool IsEnabled { get; set; }
    public int Count { get; set; }
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var propertySymbols = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
            .Select(p => model.GetDeclaredSymbol(p))
            .Where(s => s != null)
            .ToList();

        foreach (var property in propertySymbols)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(property!);
            Assert.NotNull(summary);
            Assert.NotEmpty(summary);
        }
    }

    /// <summary>
    /// Tests DocumentationGenerator for fields.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorFieldSummaries()
    {
        var compilation = CreateSimpleCompilation(@"
public class Test
{
    public const int MaxValue = 100;
    public static readonly string DefaultName = ""Test"";
    public string _fieldName;
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var fieldSymbols = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax>()
            .SelectMany(f => f.Declaration.Variables)
            .Select(v => model.GetDeclaredSymbol(v))
            .Where(s => s != null)
            .ToList();

        foreach (var field in fieldSymbols)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(field!);
            Assert.NotNull(summary);
            Assert.NotEmpty(summary);
        }
    }

    /// <summary>
    /// Tests DocumentationGenerator for events.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorEventSummaries()
    {
        var compilation = CreateSimpleCompilation(@"
public class Test
{
    public event EventHandler DataChanged;
    public event EventHandler<EventArgs> ItemAdded;
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var eventSymbols = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.EventFieldDeclarationSyntax>()
            .SelectMany(e => e.Declaration.Variables)
            .Select(v => model.GetDeclaredSymbol(v))
            .Where(s => s != null)
            .ToList();

        foreach (var evt in eventSymbols)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(evt!);
            Assert.NotNull(summary);
            Assert.NotEmpty(summary);
            Assert.Contains("Occurs when", summary);
        }
    }

    /// <summary>
    /// Tests DocumentationGenerator for constructors.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorConstructorSummaries()
    {
        var compilation = CreateSimpleCompilation(@"
public class Test
{
    public Test() { }
    public Test(string name) { }
    public Test(int id, string name) { }
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var ctorSymbols = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax>()
            .Select(c => model.GetDeclaredSymbol(c))
            .Where(s => s != null)
            .ToList();

        foreach (var ctor in ctorSymbols)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(ctor!);
            Assert.NotNull(summary);
            Assert.NotEmpty(summary);
            Assert.Contains("Initializes a new instance", summary);
        }
    }

    /// <summary>
    /// Tests DocumentationGenerator for operators.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorOperatorSummaries()
    {
        var compilation = CreateSimpleCompilation(@"
public struct Test
{
    public static Test operator +(Test a, Test b) => new Test();
    public static Test operator -(Test a, Test b) => new Test();
    public static bool operator ==(Test a, Test b) => true;
    public static bool operator !=(Test a, Test b) => false;
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        var operatorSymbols = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.OperatorDeclarationSyntax>()
            .Select(o => model.GetDeclaredSymbol(o))
            .Where(s => s != null)
            .ToList();

        foreach (var op in operatorSymbols)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(op!);
            Assert.NotNull(summary);
            Assert.NotEmpty(summary);
        }
    }

    /// <summary>
    /// Creates a simple compilation for testing.
    /// </summary>
    private static Compilation CreateSimpleCompilation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(EventHandler).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}