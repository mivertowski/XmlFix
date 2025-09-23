using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using XmlFix.Analyzers;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Comprehensive tests for MissingXmlDocsAnalyzer.
/// </summary>
public class ComprehensiveAnalyzerTests
{
    private const string TestCode = @"
namespace TestNamespace
{
    // Public class without docs - should trigger
    public class TestClass
    {
        // Public method without docs - should trigger
        public void PublicMethod() { }

        // Private method - should not trigger
        private void PrivateMethod() { }

        // Public property without docs - should trigger
        public string PublicProperty { get; set; }

        // Public field without docs - should trigger
        public string PublicField;

        // Public event without docs - should trigger
        public event EventHandler PublicEvent;

        /// <summary>
        /// Method with docs - should not trigger
        /// </summary>
        public void MethodWithDocs() { }
    }

    // Interface without docs - should trigger
    public interface ITestInterface
    {
        void InterfaceMethod();
    }

    // Enum without docs - should trigger
    public enum TestEnum
    {
        Value1,
        Value2
    }

    // Delegate without docs - should trigger
    public delegate void TestDelegate();

    // Internal class - should not trigger
    internal class InternalClass
    {
        public void Method() { }
    }

    /// <summary>
    /// Class with docs
    /// </summary>
    public class ClassWithDocs
    {
        /// <inheritdoc/>
        public virtual void VirtualMethod() { }
    }

    public class DerivedClass : ClassWithDocs
    {
        // Override without docs - should suggest inheritdoc
        public override void VirtualMethod() { }
    }

    public class ImplementingClass : ITestInterface
    {
        // Interface implementation - should suggest inheritdoc
        public void InterfaceMethod() { }
    }
}";

    /// <summary>
    /// Tests analyzer initialization.
    /// </summary>
    [Fact]
    public async Task AnalyzerInitializesCorrectly()
    {
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(TestCode);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        // Should not throw
        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
        Assert.NotNull(diagnostics);
    }

    /// <summary>
    /// Tests that analyzer detects missing documentation.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocumentation()
    {
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(TestCode);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Should have multiple XDOC001 diagnostics
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, d => Assert.Equal("XDOC001", d.Id));
    }

    /// <summary>
    /// Tests that analyzer detects missing docs on class.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocsOnClass()
    {
        var code = @"
namespace Test
{
    public class MyClass { }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Single(diagnostics);
        Assert.Equal("XDOC001", diagnostics[0].Id);
        Assert.Contains("MyClass", diagnostics[0].GetMessage());
    }

    /// <summary>
    /// Tests that analyzer detects missing docs on method.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocsOnMethod()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public void MyMethod() { }
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Single(diagnostics);
        Assert.Contains("MyMethod", diagnostics[0].GetMessage());
    }

    /// <summary>
    /// Tests that analyzer detects missing docs on property.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocsOnProperty()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public string MyProperty { get; set; }
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Single(diagnostics);
        Assert.Contains("MyProperty", diagnostics[0].GetMessage());
    }

    /// <summary>
    /// Tests that analyzer ignores private members.
    /// </summary>
    [Fact]
    public async Task IgnoresPrivateMembers()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        private void PrivateMethod() { }
        private string PrivateProperty { get; set; }
        private string privateField;
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Empty(diagnostics);
    }

    /// <summary>
    /// Tests that analyzer ignores members with existing docs.
    /// </summary>
    [Fact]
    public async Task IgnoresMembersWithDocs()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        /// <summary>Method</summary>
        public void MyMethod() { }

        /// <summary>Property</summary>
        public string MyProperty { get; set; }
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Empty(diagnostics);
    }

    /// <summary>
    /// Tests that analyzer ignores members with inheritdoc.
    /// </summary>
    [Fact]
    public async Task IgnoresMembersWithInheritdoc()
    {
        var code = @"
namespace Test
{
    /// <summary>Base</summary>
    public class BaseClass
    {
        /// <summary>Method</summary>
        public virtual void MyMethod() { }
    }

    /// <summary>Derived</summary>
    public class DerivedClass : BaseClass
    {
        /// <inheritdoc/>
        public override void MyMethod() { }
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Empty(diagnostics);
    }

    /// <summary>
    /// Tests that analyzer detects missing docs on interface.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocsOnInterface()
    {
        var code = @"
namespace Test
{
    public interface IMyInterface
    {
        void Method();
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Interface and method should both trigger
        Assert.Equal(2, diagnostics.Length);
    }

    /// <summary>
    /// Tests that analyzer detects missing docs on enum.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocsOnEnum()
    {
        var code = @"
namespace Test
{
    public enum MyEnum
    {
        Value1,
        Value2
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Enum and fields should trigger
        Assert.Equal(3, diagnostics.Length);
    }

    /// <summary>
    /// Tests that analyzer detects missing docs on delegate.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocsOnDelegate()
    {
        var code = @"
namespace Test
{
    public delegate void MyDelegate(string param);
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Single(diagnostics);
        Assert.Contains("MyDelegate", diagnostics[0].GetMessage());
    }

    /// <summary>
    /// Tests that analyzer detects missing docs on event.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocsOnEvent()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public event EventHandler MyEvent;
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Single(diagnostics);
        Assert.Contains("MyEvent", diagnostics[0].GetMessage());
    }

    /// <summary>
    /// Tests that analyzer ignores compiler-generated members.
    /// </summary>
    [Fact]
    public async Task IgnoresCompilerGenerated()
    {
        var code = @"
namespace Test
{
    /// <summary>Record</summary>
    public record MyRecord(string Name, int Age);
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Should only report on explicitly declared public members, not compiler-generated
        Assert.True(diagnostics.Length <= 2); // Record properties might trigger
    }

    /// <summary>
    /// Tests analyzer with struct.
    /// </summary>
    [Fact]
    public async Task DetectsMissingDocsOnStruct()
    {
        var code = @"
namespace Test
{
    public struct MyStruct
    {
        public int Value;
    }
}";
        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = await CreateCompilationAsync(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        Assert.Equal(2, diagnostics.Length); // Struct and field
    }

    /// <summary>
    /// Creates a compilation for testing.
    /// </summary>
    private static async Task<Compilation> CreateCompilationAsync(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await Task.FromResult(compilation);
    }
}