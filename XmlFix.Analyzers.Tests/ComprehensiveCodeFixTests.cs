using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Xunit;
using XmlFix.Analyzers;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Comprehensive tests for MissingXmlDocsCodeFix.
/// </summary>
public class ComprehensiveCodeFixTests
{
    /// <summary>
    /// Tests that code fix provider is properly configured.
    /// </summary>
    [Fact]
    public void CodeFixProviderConfiguration()
    {
        var codeFix = new MissingXmlDocsCodeFix();

        Assert.Contains("XDOC001", codeFix.FixableDiagnosticIds);
        Assert.NotNull(codeFix.GetFixAllProvider());
    }

    /// <summary>
    /// Tests code fix for class documentation.
    /// </summary>
    [Fact]
    public async Task FixesClassDocumentation()
    {
        var code = @"
namespace Test
{
    public class MyClass { }
}";

        var expected = @"
namespace Test
{
    /// <summary>
    /// A class that represents my class.
    /// </summary>
    public class MyClass { }
}";

        await VerifyCodeFixAsync(code, expected, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for method documentation.
    /// </summary>
    [Fact]
    public async Task FixesMethodDocumentation()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public void DoSomething(string input) { }
    }
}";

        var expectedContains = "/// <summary>";
        var expectedContains2 = "/// <param name=\"input\">";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for property documentation.
    /// </summary>
    [Fact]
    public async Task FixesPropertyDocumentation()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public string Name { get; set; }
    }
}";

        var expectedContains = "/// <summary>";
        var expectedContains2 = "/// <value>";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for interface documentation.
    /// </summary>
    [Fact]
    public async Task FixesInterfaceDocumentation()
    {
        var code = @"
namespace Test
{
    public interface IMyInterface
    {
        void Method();
    }
}";

        var expectedContains = "/// <summary>";
        var expectedContains2 = "interface";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for enum documentation.
    /// </summary>
    [Fact]
    public async Task FixesEnumDocumentation()
    {
        var code = @"
namespace Test
{
    public enum Status
    {
        Active,
        Inactive
    }
}";

        var expectedContains = "/// <summary>";
        var expectedContains2 = "enumeration";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for delegate documentation.
    /// </summary>
    [Fact]
    public async Task FixesDelegateDocumentation()
    {
        var code = @"
namespace Test
{
    public delegate void MyDelegate(string message);
}";

        var expectedContains = "/// <summary>";
        var expectedContains2 = "/// <param name=\"message\">";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for event documentation.
    /// </summary>
    [Fact]
    public async Task FixesEventDocumentation()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public event EventHandler DataChanged;
    }
}";

        var expectedContains = "/// <summary>";
        var expectedContains2 = "Occurs when";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
    }

    /// <summary>
    /// Tests inheritdoc suggestion for override.
    /// </summary>
    [Fact]
    public async Task SuggestsInheritdocForOverride()
    {
        var code = @"
namespace Test
{
    /// <summary>Base</summary>
    public class BaseClass
    {
        /// <summary>Virtual method</summary>
        public virtual void Method() { }
    }

    /// <summary>Derived</summary>
    public class DerivedClass : BaseClass
    {
        public override void Method() { }
    }
}";

        var expectedContains = "/// <inheritdoc/>";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add <inheritdoc/>");
    }

    /// <summary>
    /// Tests inheritdoc suggestion for interface implementation.
    /// </summary>
    [Fact]
    public async Task SuggestsInheritdocForInterfaceImplementation()
    {
        var code = @"
namespace Test
{
    /// <summary>Interface</summary>
    public interface IMyInterface
    {
        /// <summary>Method</summary>
        void Method();
    }

    /// <summary>Implementation</summary>
    public class MyClass : IMyInterface
    {
        public void Method() { }
    }
}";

        var expectedContains = "/// <inheritdoc/>";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add <inheritdoc/>");
    }

    /// <summary>
    /// Tests code fix for method with return value.
    /// </summary>
    [Fact]
    public async Task FixesMethodWithReturn()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public string GetName() { return """"; }
    }
}";

        var expectedContains = "/// <returns>";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for method with multiple parameters.
    /// </summary>
    [Fact]
    public async Task FixesMethodWithMultipleParams()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public void Process(string name, int age, bool active) { }
    }
}";

        var expectedContains1 = "/// <param name=\"name\">";
        var expectedContains2 = "/// <param name=\"age\">";
        var expectedContains3 = "/// <param name=\"active\">";

        await VerifyCodeFixContainsAsync(code, expectedContains1, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains3, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for generic method.
    /// </summary>
    [Fact]
    public async Task FixesGenericMethod()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public T Process<T>(T input) { return input; }
    }
}";

        var expectedContains1 = "/// <typeparam name=\"T\">";
        var expectedContains2 = "/// <param name=\"input\">";
        var expectedContains3 = "/// <returns>";

        await VerifyCodeFixContainsAsync(code, expectedContains1, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains3, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for constructor.
    /// </summary>
    [Fact]
    public async Task FixesConstructor()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public MyClass(string name) { }
    }
}";

        var expectedContains1 = "/// <summary>";
        var expectedContains2 = "Initializes a new instance";
        var expectedContains3 = "/// <param name=\"name\">";

        await VerifyCodeFixContainsAsync(code, expectedContains1, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains3, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for field.
    /// </summary>
    [Fact]
    public async Task FixesField()
    {
        var code = @"
namespace Test
{
    /// <summary>Class</summary>
    public class MyClass
    {
        public const string DefaultName = ""Default"";
    }
}";

        var expectedContains = "/// <summary>";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
    }

    /// <summary>
    /// Tests code fix for struct.
    /// </summary>
    [Fact]
    public async Task FixesStruct()
    {
        var code = @"
namespace Test
{
    public struct Point
    {
        public int X;
        public int Y;
    }
}";

        var expectedContains = "/// <summary>";
        var expectedContains2 = "structure";

        await VerifyCodeFixContainsAsync(code, expectedContains, "Add XML documentation");
        await VerifyCodeFixContainsAsync(code, expectedContains2, "Add XML documentation");
    }

    /// <summary>
    /// Verifies that a code fix produces the expected result.
    /// </summary>
    private static async Task VerifyCodeFixAsync(string source, string expected, string codeActionTitle)
    {
        var analyzer = new MissingXmlDocsAnalyzer();
        var codeFixProvider = new MissingXmlDocsCodeFix();

        var document = CreateDocument(source);
        var compilation = await document.Project.GetCompilationAsync();
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        if (!diagnostics.Any())
        {
            throw new InvalidOperationException("No diagnostics found");
        }

        var actions = new System.Collections.Generic.List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostics[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        var action = actions.FirstOrDefault(a => a.Title == codeActionTitle);
        Assert.NotNull(action);

        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var operation = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().FirstOrDefault();
        var newDoc = operation?.ChangedSolution?.GetDocument(document.Id);

        if (newDoc != null)
        {
            var newText = await newDoc.GetTextAsync();
            var actualText = newText.ToString();

            // Normalize whitespace for comparison
            actualText = actualText.Trim();
            expected = expected.Trim();

            Assert.Equal(expected, actualText);
        }
    }

    /// <summary>
    /// Verifies that a code fix contains expected text.
    /// </summary>
    private static async Task VerifyCodeFixContainsAsync(string source, string expectedContains, string codeActionTitle)
    {
        var analyzer = new MissingXmlDocsAnalyzer();
        var codeFixProvider = new MissingXmlDocsCodeFix();

        var document = CreateDocument(source);
        var compilation = await document.Project.GetCompilationAsync();
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        if (!diagnostics.Any())
        {
            throw new InvalidOperationException("No diagnostics found");
        }

        var actions = new System.Collections.Generic.List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostics[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        var action = actions.FirstOrDefault(a => a.Title == codeActionTitle);
        Assert.NotNull(action);

        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var operation = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().FirstOrDefault();
        var newDoc = operation?.ChangedSolution?.GetDocument(document.Id);

        if (newDoc != null)
        {
            var newText = await newDoc.GetTextAsync();
            var actualText = newText.ToString();
            Assert.Contains(expectedContains, actualText);
        }
    }

    /// <summary>
    /// Creates a document for testing.
    /// </summary>
    private static Document CreateDocument(string source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = new AdhocWorkspace()
            .CurrentSolution
            .AddProject(projectId, "TestProject", "TestAssembly", LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddDocument(documentId, "Test.cs", source);

        return solution.GetDocument(documentId);
    }
}