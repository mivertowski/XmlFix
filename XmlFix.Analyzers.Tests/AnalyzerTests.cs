using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    XmlFix.Analyzers.MissingXmlDocsAnalyzer>;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Comprehensive tests for the MissingXmlDocsAnalyzer ensuring 90%+ coverage.
/// </summary>
public class AnalyzerTests
{
    /// <summary>
    /// Tests that no diagnostics are reported for empty code.
    /// </summary>
    [Fact]
    public async Task EmptyCode_NoDiagnostics()
    {
        var test = @"";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for private members.
    /// </summary>
    [Fact]
    public async Task PrivateMembers_NoDiagnostics()
    {
        var test = @"
namespace TestNamespace
{
    class PrivateClass { }

    public class PublicClass
    {
        private void PrivateMethod() { }
        private string PrivateProperty { get; set; }
        private string privateField;
        private event EventHandler PrivateEvent;
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that diagnostics are reported for public class without documentation.
    /// </summary>
    [Fact]
    public async Task PublicClass_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class [|TestClass|] { }
}";

        var expected = VerifyCS.Diagnostic("XDOC001")
            .WithLocation(4, 18)
            .WithArguments("class", "TestClass");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for public class with documentation.
    /// </summary>
    [Fact]
    public async Task PublicClass_WithDocs_NoDiagnostics()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass { }
}";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests multiple public members without documentation.
    /// </summary>
    [Fact]
    public async Task MultiplePublicMembers_WithoutDocs_ReportsMultipleDiagnostics()
    {
        var test = @"
namespace TestNamespace
{
    public class [|TestClass|]
    {
        public void [|Method|]() { }
        public string [|Property|] { get; set; }
        public const string [|ConstField|] = ""value"";
        public event EventHandler [|Event|];
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that override methods don't require documentation.
    /// </summary>
    [Fact]
    public async Task OverrideMethod_NoDiagnostics()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Base class.
    /// </summary>
    public abstract class BaseClass
    {
        /// <summary>
        /// Virtual method.
        /// </summary>
        public virtual void VirtualMethod() { }
    }

    /// <summary>
    /// Derived class.
    /// </summary>
    public class DerivedClass : BaseClass
    {
        public override void VirtualMethod() { }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests interface implementation doesn't require documentation.
    /// </summary>
    [Fact]
    public async Task InterfaceImplementation_NoDiagnostics()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test interface.
    /// </summary>
    public interface ITest
    {
        /// <summary>
        /// Interface method.
        /// </summary>
        void Method();
    }

    /// <summary>
    /// Implementation class.
    /// </summary>
    public class TestClass : ITest
    {
        public void Method() { }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests explicit interface implementation doesn't require documentation.
    /// </summary>
    [Fact]
    public async Task ExplicitInterfaceImplementation_NoDiagnostics()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test interface.
    /// </summary>
    public interface ITest
    {
        /// <summary>
        /// Interface method.
        /// </summary>
        void Method();
    }

    /// <summary>
    /// Implementation class.
    /// </summary>
    public class TestClass : ITest
    {
        void ITest.Method() { }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests various public types require documentation.
    /// </summary>
    [Theory]
    [InlineData("class")]
    [InlineData("interface")]
    [InlineData("struct")]
    [InlineData("enum")]
    [InlineData("record")]
    [InlineData("record struct")]
    public async Task PublicTypes_WithoutDocs_ReportsDiagnostic(string typeKind)
    {
        var test = $@"
namespace TestNamespace
{{
    public {typeKind} [|TestType|] {{ }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests public delegate without documentation.
    /// </summary>
    [Fact]
    public async Task PublicDelegate_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public delegate void [|TestDelegate|](int x);
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests nested public types require documentation.
    /// </summary>
    [Fact]
    public async Task NestedPublicType_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Outer class.
    /// </summary>
    public class OuterClass
    {
        public class [|InnerClass|] { }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests generic types and methods.
    /// </summary>
    [Fact]
    public async Task GenericTypesAndMethods_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public class [|GenericClass|]<T>
    {
        public void [|GenericMethod|]<U>(T t, U u) { }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests async methods.
    /// </summary>
    [Fact]
    public async Task AsyncMethod_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
using System.Threading.Tasks;

namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public async Task [|DoSomethingAsync|]()
        {
            await Task.Delay(100);
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests properties with different accessibilities.
    /// </summary>
    [Fact]
    public async Task PropertiesWithDifferentAccessibilities_RequireDocs()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public string [|PublicProperty|] { get; set; }
        public string [|GetOnlyProperty|] { get; }
        public string [|SetOnlyProperty|] { set { } }
        public string [|InitProperty|] { get; init; }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests indexers require documentation.
    /// </summary>
    [Fact]
    public async Task Indexer_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public int [|this|][int index]
        {
            get { return 0; }
            set { }
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests operators require documentation.
    /// </summary>
    [Fact]
    public async Task Operators_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public static TestClass [|operator +|](TestClass a, TestClass b)
        {
            return new TestClass();
        }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests constructors require documentation.
    /// </summary>
    [Fact]
    public async Task Constructor_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public [|TestClass|]() { }
        public [|TestClass|](int value) { }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests static members require documentation.
    /// </summary>
    [Fact]
    public async Task StaticMembers_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public static void [|StaticMethod|]() { }
        public static string [|StaticProperty|] { get; set; }
        public const string [|ConstField|] = ""value"";
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests partial classes.
    /// </summary>
    [Fact]
    public async Task PartialClass_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public partial class [|PartialClass|]
    {
        public void [|Method1|]() { }
    }

    public partial class PartialClass
    {
        public void [|Method2|]() { }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests record types with properties.
    /// </summary>
    [Fact]
    public async Task RecordTypes_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace
{
    public record [|Person|](string Name, int Age);

    public record struct [|Point|](double X, double Y);
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests file-scoped namespaces.
    /// </summary>
    [Fact]
    public async Task FileScopedNamespace_WithoutDocs_ReportsDiagnostic()
    {
        var test = @"
namespace TestNamespace;

public class [|TestClass|] { }
";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests generated code is ignored.
    /// </summary>
    [Fact]
    public async Task GeneratedCode_NoDiagnostics()
    {
        var test = @"
// <auto-generated />
namespace TestNamespace
{
    public class GeneratedClass { }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests compiler generated attribute is ignored.
    /// </summary>
    [Fact]
    public async Task CompilerGeneratedAttribute_NoDiagnostics()
    {
        var test = @"
using System.Runtime.CompilerServices;

namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        [CompilerGenerated]
        public string Property { get; set; }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}