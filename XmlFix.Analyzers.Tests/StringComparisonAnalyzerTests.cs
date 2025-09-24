using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Threading.Tasks;
using Xunit;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Unit tests for StringComparisonAnalyzer.
/// </summary>
public class StringComparisonAnalyzerTests
{
    /// <summary>
    /// Creates an analyzer test for StringComparisonAnalyzer.
    /// </summary>
    /// <returns>The configured analyzer test.</returns>
    private static CSharpAnalyzerTest<StringComparisonAnalyzer, DefaultVerifier> CreateTest()
    {
        return new CSharpAnalyzerTest<StringComparisonAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };
    }

    /// <summary>
    /// Tests that string.StartsWith without StringComparison triggers diagnostic.
    /// </summary>
    [Fact]
    public async Task StringStartsWith_WithoutComparison_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool CheckPrefix(string text)
    {
        return text.{|XFIX002:StartsWith|}(""prefix"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that string.EndsWith without StringComparison triggers diagnostic.
    /// </summary>
    [Fact]
    public async Task StringEndsWith_WithoutComparison_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool CheckSuffix(string text)
    {
        return text.{|XFIX002:EndsWith|}("".txt"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that string.Equals without StringComparison triggers diagnostic.
    /// </summary>
    [Fact]
    public async Task StringEquals_WithoutComparison_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool CheckEquality(string text)
    {
        return text.{|XFIX002:Equals|}(""target"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that string.Contains without StringComparison triggers diagnostic.
    /// </summary>
    [Fact]
    public async Task StringContains_WithoutComparison_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool CheckContains(string text)
    {
        return text.{|XFIX002:Contains|}(""search"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that string.StartsWith with StringComparison does not trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task StringStartsWith_WithComparison_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using System;

public class TestClass
{
    public bool CheckPrefix(string text)
    {
        return text.StartsWith(""prefix"", StringComparison.Ordinal);
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that non-string methods do not trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task NonStringMethod_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool StartsWith(string value)
    {
        return true;
    }

    public bool Test()
    {
        return StartsWith(""test"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that string methods on non-string types do not trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task StringMethodOnNonStringType_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class CustomType
{
    public bool StartsWith(string value) => true;
}

public class TestClass
{
    public bool Test()
    {
        var custom = new CustomType();
        return custom.StartsWith(""test"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests multiple string comparison methods in one class.
    /// </summary>
    [Fact]
    public async Task MultipleStringMethods_ShouldAllTriggerDiagnostics()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool CheckText(string text)
    {
        return text.{|XFIX002:StartsWith|}(""start"") &&
               text.{|XFIX002:EndsWith|}(""end"") &&
               text.{|XFIX002:Contains|}(""middle"") &&
               text.{|XFIX002:Equals|}(""exact"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests static String.Compare method.
    /// </summary>
    [Fact]
    public async Task StaticStringCompare_WithoutComparison_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public int CompareStrings(string a, string b)
    {
        return string.{|XFIX002:Compare|}(a, b);
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests static String.Compare with StringComparison.
    /// </summary>
    [Fact]
    public async Task StaticStringCompare_WithComparison_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using System;

public class TestClass
{
    public int CompareStrings(string a, string b)
    {
        return string.Compare(a, b, StringComparison.Ordinal);
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests string.IndexOf method without StringComparison.
    /// </summary>
    [Fact]
    public async Task StringIndexOf_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public int FindText(string text)
    {
        return text.{|XFIX002:IndexOf|}(""other"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods in culture-specific contexts are not flagged.
    /// </summary>
    [Fact]
    public async Task MethodInCultureContext_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using System.Globalization;

public class TestClass
{
    public bool DisplayText(string userText, CultureInfo culture)
    {
        return userText.StartsWith(""Hello"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that methods in UI contexts are still flagged for StringComparison.
    /// </summary>
    [Fact]
    public async Task MethodInUIContext_MightNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool DisplayUserMessage(string message)
    {
        return message.{|XFIX002:StartsWith|}(""Error"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests string interpolation with string methods.
    /// </summary>
    [Fact]
    public async Task StringMethodInInterpolation_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public string FormatMessage(string text)
    {
        return $""Result: {text.{|XFIX002:StartsWith|}(""prefix"")}"";
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests nested string method calls.
    /// </summary>
    [Fact]
    public async Task NestedStringMethods_ShouldTriggerDiagnostics()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool ComplexCheck(string text)
    {
        return text.ToLower().{|XFIX002:StartsWith|}(""prefix"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests conditional string method calls.
    /// </summary>
    [Fact]
    public async Task ConditionalStringMethod_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool CheckText(string text)
    {
        return !string.IsNullOrEmpty(text) && text.{|XFIX002:EndsWith|}("".log"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests string methods in LINQ expressions.
    /// </summary>
    [Fact]
    public async Task StringMethodInLinq_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using System.Linq;

public class TestClass
{
    public bool HasMatchingItems(string[] items, string prefix)
    {
        return items.Any(item => item.{|XFIX002:StartsWith|}(prefix));
    }
}";

        await test.RunAsync();
    }
}