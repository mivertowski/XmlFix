using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Threading.Tasks;
using Xunit;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Unit tests for PropertyMethodCodeFix.
/// </summary>
public class PropertyMethodCodeFixTests
{
    /// <summary>
    /// Creates a code fix test for PropertyMethodCodeFix.
    /// </summary>
    /// <returns>The configured code fix test.</returns>
    private static CSharpCodeFixTest<PropertyMethodAnalyzer, PropertyMethodCodeFix, DefaultVerifier> CreateTest()
    {
        return new CSharpCodeFixTest<PropertyMethodAnalyzer, PropertyMethodCodeFix, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };
    }

    /// <summary>
    /// Tests converting a simple getter method to a property.
    /// </summary>
    [Fact]
    public async Task ConvertSimpleGetterToProperty()
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

        test.FixedCode = @"
public class TestClass
{
    private string _name;

    public string Name
    {
        get
        {
            return _name;
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting an expression-bodied method to a property.
    /// </summary>
    [Fact]
    public async Task ConvertExpressionBodiedMethodToProperty()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;

    public string {|XFIX001:GetName|}() => _name;
}";

        test.FixedCode = @"
public class TestClass
{
    private string _name;

    public string Name => _name;
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting a getter method to an auto-property.
    /// </summary>
    [Fact]
    public async Task ConvertSimpleGetterToAutoProperty()
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

        test.FixedCode = @"
public class TestClass
{
    private string _name;

    public string Name { get; set; }
}";

        test.CodeActionEquivalenceKey = "ConvertToAutoProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting a boolean method starting with 'Is'.
    /// </summary>
    [Fact]
    public async Task ConvertIsBooleanMethod()
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

        test.FixedCode = @"
public class TestClass
{
    private bool _valid;

    public bool Valid
    {
        get
        {
            return _valid;
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting a method starting with 'Has'.
    /// </summary>
    [Fact]
    public async Task ConvertHasMethod()
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

        test.FixedCode = @"
public class TestClass
{
    private bool _children;

    public bool Children
    {
        get
        {
            return _children;
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting a method starting with 'Can'.
    /// </summary>
    [Fact]
    public async Task ConvertCanMethod()
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

        test.FixedCode = @"
public class TestClass
{
    private bool _execute;

    public bool Execute
    {
        get
        {
            return _execute;
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests preserving method modifiers when converting to property.
    /// </summary>
    [Fact]
    public async Task PreserveMethodModifiers()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;

    protected internal string {|XFIX001:GetName|}()
    {
        return _name;
    }
}";

        test.FixedCode = @"
public class TestClass
{
    private string _name;

    protected internal string Name
    {
        get
        {
            return _name;
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting a method with conditional return.
    /// </summary>
    [Fact]
    public async Task ConvertMethodWithConditionalReturn()
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

        test.FixedCode = @"
public class TestClass
{
    private string _name;
    private bool _hasName;

    public string DisplayName
    {
        get
        {
            return _hasName ? _name : ""No Name"";
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting an expression-bodied method with conditional.
    /// </summary>
    [Fact]
    public async Task ConvertExpressionBodiedConditionalMethod()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;
    private bool _hasName;

    public string {|XFIX001:GetDisplayName|}() => _hasName ? _name : ""No Name"";
}";

        test.FixedCode = @"
public class TestClass
{
    private string _name;
    private bool _hasName;

    public string DisplayName => _hasName ? _name : ""No Name"";
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests preserving XML documentation comments when converting.
    /// </summary>
    [Fact]
    public async Task PreserveXmlDocumentation()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;

    /// <summary>
    /// Gets the name of the object.
    /// </summary>
    /// <returns>The name.</returns>
    public string {|XFIX001:GetName|}()
    {
        return _name;
    }
}";

        test.FixedCode = @"
public class TestClass
{
    private string _name;

    /// <summary>
    /// Gets the name of the object.
    /// </summary>
    /// <returns>The name.</returns>
    public string Name
    {
        get
        {
            return _name;
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests handling method names without common prefixes.
    /// </summary>
    [Fact]
    public async Task ConvertMethodWithoutCommonPrefix()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _value;

    public string {|XFIX001:Value|}()
    {
        return _value;
    }
}";

        test.FixedCode = @"
public class TestClass
{
    private string _value;

    public string Value
    {
        get
        {
            return _value;
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting multiple methods in the same class.
    /// </summary>
    [Fact]
    public async Task ConvertMultipleMethodsInClass()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    private string _name;
    private int _id;

    public string {|XFIX001:GetName|}()
    {
        return _name;
    }

    public int {|XFIX001:GetId|}()
    {
        return _id;
    }
}";

        test.FixedCode = @"
public class TestClass
{
    private string _name;
    private int _id;

    public string Name
    {
        get
        {
            return _name;
        }
    }

    public int Id
    {
        get
        {
            return _id;
        }
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToProperty";
        await test.RunAsync();
    }
}