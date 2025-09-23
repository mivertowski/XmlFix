using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<XmlFix.Analyzers.MissingXmlDocsAnalyzer>;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Tests for the MissingXmlDocsAnalyzer.
/// </summary>
public class MissingXmlDocsAnalyzerTests
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
    /// Tests that diagnostics are reported for public class without XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicClass_WithoutXmlDocs_ReportsDiagnostic()
    {
        var test = @"
public class {|XDOC001:TestClass|}
{
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for public class with XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicClass_WithXmlDocs_NoDiagnostics()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that diagnostics are reported for public method without XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicMethod_WithoutXmlDocs_ReportsDiagnostic()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public void {|XDOC001:TestMethod|}() { }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for public method with XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicMethod_WithXmlDocs_NoDiagnostics()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    /// <summary>
    /// A test method.
    /// </summary>
    public void TestMethod() { }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that diagnostics are reported for public property without XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicProperty_WithoutXmlDocs_ReportsDiagnostic()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public string {|XDOC001:TestProperty|} { get; set; }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for private members.
    /// </summary>
    [Fact]
    public async Task PrivateMembers_NoDiagnostics()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    private void PrivateMethod() { }
    private string PrivateProperty { get; set; }
    private string privateField;
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that diagnostics are reported for public const field without XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicConstField_WithoutXmlDocs_ReportsDiagnostic()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public const string {|XDOC001:TestConstant|} = ""test"";
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for public regular field.
    /// </summary>
    [Fact]
    public async Task PublicField_NoDiagnostics()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public string TestField;
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that diagnostics are reported for public interface without XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicInterface_WithoutXmlDocs_ReportsDiagnostic()
    {
        var test = @"
public interface {|XDOC001:ITestInterface|}
{
    void {|XDOC001:TestMethod|}();
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that diagnostics are reported for public enum without XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicEnum_WithoutXmlDocs_ReportsDiagnostic()
    {
        var test = @"
public enum {|XDOC001:TestEnum|}
{
    {|XDOC001:Value1|},
    {|XDOC001:Value2|}
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that diagnostics are reported for public event without XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicEvent_WithoutXmlDocs_ReportsDiagnostic()
    {
        var test = @"
using System;

/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public event Action {|XDOC001:TestEvent|};
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that diagnostics are reported for public delegate without XML documentation.
    /// </summary>
    [Fact]
    public async Task PublicDelegate_WithoutXmlDocs_ReportsDiagnostic()
    {
        var test = @"
public delegate void {|XDOC001:TestDelegate|}(string message);";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for override methods (should use inheritdoc).
    /// </summary>
    [Fact]
    public async Task OverrideMethod_NoDiagnostics()
    {
        var test = @"
/// <summary>
/// A base class.
/// </summary>
public class BaseClass
{
    /// <summary>
    /// A virtual method.
    /// </summary>
    public virtual void VirtualMethod() { }
}

/// <summary>
/// A derived class.
/// </summary>
public class DerivedClass : BaseClass
{
    public override void VirtualMethod() { }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for interface implementations (should use inheritdoc).
    /// </summary>
    [Fact]
    public async Task InterfaceImplementation_NoDiagnostics()
    {
        var test = @"
/// <summary>
/// A test interface.
/// </summary>
public interface ITestInterface
{
    /// <summary>
    /// A test method.
    /// </summary>
    void TestMethod();
}

/// <summary>
/// A test class.
/// </summary>
public class TestClass : ITestInterface
{
    public void TestMethod() { }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    /// <summary>
    /// Tests that no diagnostics are reported for explicit interface implementations.
    /// </summary>
    [Fact]
    public async Task ExplicitInterfaceImplementation_NoDiagnostics()
    {
        var test = @"
/// <summary>
/// A test interface.
/// </summary>
public interface ITestInterface
{
    /// <summary>
    /// A test method.
    /// </summary>
    void TestMethod();
}

/// <summary>
/// A test class.
/// </summary>
public class TestClass : ITestInterface
{
    void ITestInterface.TestMethod() { }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}