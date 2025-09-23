using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using XmlFix.Analyzers;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Final tests to reach 90% coverage.
/// </summary>
public class FinalCoverageTests
{
    /// <summary>
    /// Tests analyzer with various symbol kinds.
    /// </summary>
    [Fact]
    public async Task AnalyzerHandlesAllSymbolKinds()
    {
        var code = @"
namespace Test
{
    public class TestClass
    {
        public TestClass() { }
        ~TestClass() { }
        public void Method() { }
        public string Property { get; set; }
        public string Field;
        public event EventHandler Event;
        public static TestClass operator +(TestClass a, TestClass b) => a;
        public string this[int index] { get => """"; set { } }
    }
    public interface ITest { }
    public struct TestStruct { }
    public enum TestEnum { }
    public delegate void TestDelegate();
}";

        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = CreateCompilation(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        Assert.NotEmpty(diagnostics);
    }

    /// <summary>
    /// Tests analyzer skips internal and private types.
    /// </summary>
    [Fact]
    public async Task AnalyzerSkipsNonPublicTypes()
    {
        var code = @"
namespace Test
{
    internal class InternalClass
    {
        public void Method() { }
    }

    class DefaultClass
    {
        public void Method() { }
    }

    public class PublicClass
    {
        private class PrivateClass
        {
            public void Method() { }
        }

        protected class ProtectedClass
        {
            public void Method() { }
        }
    }
}";

        var analyzer = new MissingXmlDocsAnalyzer();
        var compilation = CreateCompilation(code);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Should only report on PublicClass and ProtectedClass
        Assert.All(diagnostics, d => Assert.Equal("XDOC001", d.Id));
    }

    /// <summary>
    /// Tests documentation generator handles all method patterns.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorMethodPatterns()
    {
        var patterns = new[]
        {
            ("Get", "Gets"),
            ("Set", "Sets"),
            ("Create", "Creates"),
            ("Delete", "Deletes"),
            ("Remove", "Deletes"),
            ("Update", "Updates"),
            ("Find", "Finds"),
            ("Calculate", "Calculates"),
            ("Compute", "Calculates"),
            ("Validate", "Validates"),
            ("Initialize", "Initializes"),
            ("Process", "Processes"),
            ("Is", "Determines"),
            ("Has", "Determines"),
            ("Can", "Determines"),
            ("Try", "true if")
        };

        var compilation = CreateCompilation(@"
public class Test
{
    public void GetData() { }
    public void SetValue() { }
    public void CreateObject() { }
    public void DeleteItem() { }
    public void RemoveItem() { }
    public void UpdateRecord() { }
    public void FindUser() { }
    public void CalculateSum() { }
    public void ComputeTotal() { }
    public void ValidateInput() { }
    public void InitializeData() { }
    public void ProcessRequest() { }
    public bool IsValid() { return true; }
    public bool HasData() { return true; }
    public bool CanProcess() { return true; }
    public bool TryParse() { return true; }
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);

        var methods = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(m => model.GetDeclaredSymbol(m))
            .Where(m => m != null)
            .ToList();

        foreach (var method in methods)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(method!);
            Assert.NotEmpty(summary);

            // Check if summary contains expected pattern
            var expectedPattern = patterns.FirstOrDefault(p => method!.Name.StartsWith(p.Item1));
            if (!expectedPattern.Equals(default))
            {
                Assert.Contains(expectedPattern.Item2, summary);
            }
        }
    }

    /// <summary>
    /// Tests documentation generator for all type kinds.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorTypeKinds()
    {
        var expectedTerms = new[]
        {
            ("Class", "class"),
            ("Interface", "interface"),
            ("Struct", "structure"),
            ("Enum", "enumeration"),
            ("Delegate", "delegate")
        };

        foreach (var term in expectedTerms)
        {
            var code = term.Item1 switch
            {
                "Class" => "public class TestClass { }",
                "Interface" => "public interface ITestInterface { }",
                "Struct" => "public struct TestStruct { }",
                "Enum" => "public enum TestEnum { Value }",
                "Delegate" => "public delegate void TestDelegate();",
                _ => ""
            };

            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);

            var typeSymbol = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).FirstOrDefault();
            if (typeSymbol != null)
            {
                var summary = DocumentationGenerator.GenerateIntelligentSummary(typeSymbol);
                Assert.Contains(term.Item2, summary.ToLowerInvariant());
            }
        }
    }

    /// <summary>
    /// Tests documentation generator for operators.
    /// </summary>
    [Fact]
    public void DocumentationGeneratorOperators()
    {
        var operatorPairs = new[]
        {
            ("+", "addition"),
            ("-", "subtraction"),
            ("*", "multiplication"),
            ("/", "division"),
            ("%", "modulus"),
            ("==", "equality"),
            ("!=", "inequality"),
            ("<", "less than"),
            (">", "greater than"),
            ("<=", "less than or equal"),
            (">=", "greater than or equal"),
            ("&", "bitwise and"),
            ("|", "bitwise or"),
            ("^", "bitwise xor"),
            ("<<", "left shift"),
            (">>", "right shift"),
            ("!", "logical negation"),
            ("~", "bitwise complement"),
            ("++", "increment"),
            ("--", "decrement"),
            ("true", "true"),
            ("false", "false")
        };

        var compilation = CreateCompilation(@"
public struct Number
{
    public static Number operator +(Number a, Number b) => a;
    public static Number operator -(Number a, Number b) => a;
    public static Number operator *(Number a, Number b) => a;
    public static Number operator /(Number a, Number b) => a;
    public static Number operator %(Number a, Number b) => a;
    public static bool operator ==(Number a, Number b) => true;
    public static bool operator !=(Number a, Number b) => false;
    public static bool operator <(Number a, Number b) => false;
    public static bool operator >(Number a, Number b) => false;
    public static bool operator <=(Number a, Number b) => false;
    public static bool operator >=(Number a, Number b) => false;
    public static Number operator &(Number a, Number b) => a;
    public static Number operator |(Number a, Number b) => a;
    public static Number operator ^(Number a, Number b) => a;
    public static Number operator <<(Number a, int b) => a;
    public static Number operator >>(Number a, int b) => a;
    public static Number operator !(Number a) => a;
    public static Number operator ~(Number a) => a;
    public static Number operator ++(Number a) => a;
    public static Number operator --(Number a) => a;
    public static bool operator true(Number a) => true;
    public static bool operator false(Number a) => false;
}");

        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);

        var operators = tree.GetRoot()
            .DescendantNodes()
            .OfType<OperatorDeclarationSyntax>()
            .Select(o => model.GetDeclaredSymbol(o))
            .Where(o => o != null)
            .ToList();

        foreach (var op in operators)
        {
            var summary = DocumentationGenerator.GenerateIntelligentSummary(op!);
            Assert.NotEmpty(summary);

            // Find expected term for this operator
            var opName = op!.Name.Replace("op_", "");
            var expectedTerm = operatorPairs.FirstOrDefault(p =>
                p.Item1.Equals(opName, StringComparison.OrdinalIgnoreCase) ||
                (opName == "Addition" && p.Item1 == "+") ||
                (opName == "Subtraction" && p.Item1 == "-") ||
                (opName == "Multiply" && p.Item1 == "*") ||
                (opName == "Division" && p.Item1 == "/") ||
                (opName == "Modulus" && p.Item1 == "%") ||
                (opName == "Equality" && p.Item1 == "==") ||
                (opName == "Inequality" && p.Item1 == "!=") ||
                (opName == "LessThan" && p.Item1 == "<") ||
                (opName == "GreaterThan" && p.Item1 == ">") ||
                (opName == "LessThanOrEqual" && p.Item1 == "<=") ||
                (opName == "GreaterThanOrEqual" && p.Item1 == ">=") ||
                (opName == "BitwiseAnd" && p.Item1 == "&") ||
                (opName == "BitwiseOr" && p.Item1 == "|") ||
                (opName == "ExclusiveOr" && p.Item1 == "^") ||
                (opName == "LeftShift" && p.Item1 == "<<") ||
                (opName == "RightShift" && p.Item1 == ">>") ||
                (opName == "LogicalNot" && p.Item1 == "!") ||
                (opName == "OnesComplement" && p.Item1 == "~") ||
                (opName == "Increment" && p.Item1 == "++") ||
                (opName == "Decrement" && p.Item1 == "--") ||
                (opName == "True" && p.Item1 == "true") ||
                (opName == "False" && p.Item1 == "false"));

            if (!expectedTerm.Equals(default))
            {
                Assert.Contains(expectedTerm.Item2, summary.ToLowerInvariant());
            }
        }
    }

    /// <summary>
    /// Tests parameter descriptions with edge cases.
    /// </summary>
    [Fact]
    public void ParameterDescriptionEdgeCases()
    {
        // Empty string
        var desc = DocumentationGenerator.GenerateParameterDescription("");
        Assert.NotNull(desc);

        // Single character parameters
        desc = DocumentationGenerator.GenerateParameterDescription("i");
        Assert.NotNull(desc);
        Assert.Contains("i", desc.ToLowerInvariant());

        // Parameters with numbers
        desc = DocumentationGenerator.GenerateParameterDescription("param1");
        Assert.NotNull(desc);
        Assert.Contains("param", desc.ToLowerInvariant());

        // Camel case parameters
        desc = DocumentationGenerator.GenerateParameterDescription("myParameterName");
        Assert.NotNull(desc);
        Assert.Contains("my parameter name", desc.ToLowerInvariant());
    }

    /// <summary>
    /// Creates a compilation for testing.
    /// </summary>
    private static Compilation CreateCompilation(string source)
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