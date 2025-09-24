using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Unit tests for PropertyMethodAnalyzer.
/// </summary>
public class PropertyMethodAnalyzerTests
{
    /// <summary>
    /// Creates an analyzer test for PropertyMethodAnalyzer.
    /// </summary>
    /// <returns>The configured analyzer test.</returns>
    private static CSharpAnalyzerTest<PropertyMethodAnalyzer, DefaultVerifier> CreateTest()
    {
        return new CSharpAnalyzerTest<PropertyMethodAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };
    }

    /// <summary>
    /// Tests that simple getter methods are flagged for property conversion.
    /// </summary>
    [Fact]
    public async Task SimpleGetterMethod_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;

    public string {|XFIX001:GetName|}()
    {
        return _name;
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods with parameters are not flagged.
    /// </summary>
    [Fact]
    public async Task MethodWithParameters_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public string GetName(int id)
    {
        return ""name"" + id;
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that void methods are not flagged.
    /// </summary>
    [Fact]
    public async Task VoidMethod_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public void GetData()
    {
        // Do something
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that private methods are not flagged.
    /// </summary>
    [Fact]
    public async Task PrivateMethod_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string GetName()
    {
        return ""name"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that virtual methods are not flagged.
    /// </summary>
    [Fact]
    public async Task VirtualMethod_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public virtual string GetName()
    {
        return ""name"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that override methods are not flagged.
    /// </summary>
    [Fact]
    public async Task OverrideMethod_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class BaseClass
{
    public virtual string GetName() => ""base"";
}

public class TestClass : BaseClass
{
    public override string GetName()
    {
        return ""derived"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that interface implementations are not flagged.
    /// </summary>
    [Fact]
    public async Task InterfaceImplementation_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public interface ITestInterface
{
    string GetName();
}

public class TestClass : ITestInterface
{
    public string GetName()
    {
        return ""name"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods with complex logic are not flagged.
    /// </summary>
    [Fact]
    public async Task MethodWithComplexLogic_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public string GetData()
    {
        for (int i = 0; i < 10; i++)
        {
            // Complex logic
        }
        return ""data"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods with expensive operation indicators are not flagged.
    /// </summary>
    [Fact]
    public async Task MethodWithExpensiveOperations_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public string CalculateValue()
    {
        return ""calculated"";
    }

    public string LoadData()
    {
        return ""loaded"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods with try-catch blocks are not flagged.
    /// </summary>
    [Fact]
    public async Task MethodWithTryCatch_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public string GetValue()
    {
        try
        {
            return ""value"";
        }
        catch
        {
            return ""error"";
        }
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that expression-bodied methods are flagged appropriately.
    /// </summary>
    [Fact]
    public async Task ExpressionBodiedMethod_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;

    public string {|XFIX001:GetName|}() => _name;
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods starting with 'Is' are flagged.
    /// </summary>
    [Fact]
    public async Task IsMethod_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private bool _valid;

    public bool {|XFIX001:IsValid|}()
    {
        return _valid;
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods starting with 'Has' are flagged.
    /// </summary>
    [Fact]
    public async Task HasMethod_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private bool _children;

    public bool {|XFIX001:HasChildren|}()
    {
        return _children;
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods starting with 'Can' are flagged.
    /// </summary>
    [Fact]
    public async Task CanMethod_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private bool _execute;

    public bool {|XFIX001:CanExecute|}()
    {
        return _execute;
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that static methods are not flagged.
    /// </summary>
    [Fact]
    public async Task StaticMethod_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public static string GetName()
    {
        return ""name"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods with method calls are not flagged.
    /// </summary>
    [Fact]
    public async Task MethodWithMethodCalls_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public string GetProcessedValue()
    {
        return ProcessValue(""input"");
    }

    private string ProcessValue(string input)
    {
        return input;
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods accessing external resources are not flagged.
    /// </summary>
    [Fact]
    public async Task MethodAccessingFile_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public string GetFileContent()
    {
        return System.IO.File.ReadAllText(""test.txt"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods with ObsoleteAttribute are not flagged.
    /// </summary>
    [Fact]
    public async Task ObsoleteMethod_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using System;

public class TestClass
{
    [Obsolete]
    public string GetName()
    {
        return ""name"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that multiple simple getter methods are all flagged.
    /// </summary>
    [Fact]
    public async Task MultipleSimpleGetters_ShouldAllTriggerDiagnostics()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;
    private int _id;
    private bool _active;

    public string {|XFIX001:GetName|}()
    {
        return _name;
    }

    public int {|XFIX001:GetId|}()
    {
        return _id;
    }

    public bool {|XFIX001:IsActive|}()
    {
        return _active;
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that protected methods are flagged.
    /// </summary>
    [Fact]
    public async Task ProtectedMethod_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;

    protected string {|XFIX001:GetName|}()
    {
        return _name;
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods with simple conditional expressions are flagged.
    /// </summary>
    [Fact]
    public async Task MethodWithSimpleConditional_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;
    private bool _hasName;

    public string {|XFIX001:GetDisplayName|}()
    {
        return _hasName ? _name : ""No Name"";
    }
}";

        await test.RunAsync();
    }
}