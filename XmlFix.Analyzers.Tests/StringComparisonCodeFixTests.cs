using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Threading.Tasks;
using Xunit;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Unit tests for StringComparisonCodeFix.
/// </summary>
public class StringComparisonCodeFixTests
{
    /// <summary>
    /// Creates a code fix test for StringComparisonCodeFix.
    /// </summary>
    /// <returns>The configured code fix test.</returns>
    private static CSharpCodeFixTest<StringComparisonAnalyzer, StringComparisonCodeFix, DefaultVerifier> CreateTest()
    {
        return new CSharpCodeFixTest<StringComparisonAnalyzer, StringComparisonCodeFix, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };
    }

    /// <summary>
    /// Tests adding StringComparison.Ordinal to StartsWith method.
    /// </summary>
    [Fact]
    public async Task AddOrdinalToStartsWith()
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

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool CheckPrefix(string text)
    {
        return text.StartsWith(""prefix"", StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests adding StringComparison.OrdinalIgnoreCase to EndsWith method.
    /// </summary>
    [Fact]
    public async Task AddOrdinalIgnoreCaseToEndsWith()
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

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool CheckSuffix(string text)
    {
        return text.EndsWith("".txt"", StringComparison.OrdinalIgnoreCase);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_OrdinalIgnoreCase";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests adding StringComparison to Equals method.
    /// </summary>
    [Fact]
    public async Task AddStringComparisonToEquals()
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

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool CheckEquality(string text)
    {
        return text.Equals(""target"", StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests adding StringComparison to Contains method.
    /// </summary>
    [Fact]
    public async Task AddStringComparisonToContains()
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

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool CheckContains(string text)
    {
        return text.Contains(""search"", StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests that System using directive is not duplicated.
    /// </summary>
    [Fact]
    public async Task ExistingSystemUsing_ShouldNotDuplicate()
    {
        var test = CreateTest();
        test.TestCode = @"
using System;

public class TestClass
{
    public bool CheckPrefix(string text)
    {
        return text.{|XFIX002:StartsWith|}(""prefix"");
    }
}";

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool CheckPrefix(string text)
    {
        return text.StartsWith(""prefix"", StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests fixing multiple string methods in the same method.
    /// </summary>
    [Fact]
    public async Task FixMultipleStringMethodsInSameMethod()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool CheckText(string text)
    {
        return text.{|XFIX002:StartsWith|}(""start"") && text.{|XFIX002:EndsWith|}(""end"");
    }
}";

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool CheckText(string text)
    {
        return text.StartsWith(""start"", StringComparison.Ordinal) && text.EndsWith(""end"", StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests adding StringComparison to static String.Compare method.
    /// </summary>
    [Fact]
    public async Task AddStringComparisonToStaticCompare()
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

        test.FixedCode = @"
using System;

public class TestClass
{
    public int CompareStrings(string a, string b)
    {
        return string.Compare(a, b, StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests preserving method arguments when adding StringComparison.
    /// </summary>
    [Fact]
    public async Task PreserveExistingArguments()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool CheckSubstring(string text, int startIndex)
    {
        return text.{|XFIX002:StartsWith|}(""prefix"");
    }
}";

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool CheckSubstring(string text, int startIndex)
    {
        return text.StartsWith(""prefix"", StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests adding StringComparison in complex expressions.
    /// </summary>
    [Fact]
    public async Task AddStringComparisonInComplexExpression()
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

        test.FixedCode = @"
using System;

public class TestClass
{
    public string FormatMessage(string text)
    {
        return $""Result: {text.StartsWith(""prefix"", StringComparison.Ordinal)}"";
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests fixing string methods in LINQ expressions.
    /// </summary>
    [Fact]
    public async Task FixStringMethodInLinq()
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

        test.FixedCode = @"
using System;
using System.Linq;

public class TestClass
{
    public bool HasMatchingItems(string[] items, string prefix)
    {
        return items.Any(item => item.StartsWith(prefix, StringComparison.Ordinal));
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests adding CurrentCulture comparison for user-facing text.
    /// </summary>
    [Fact]
    public async Task AddCurrentCultureForUserText()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool DisplayUserMessage(string userText)
    {
        return userText.{|XFIX002:StartsWith|}(""Welcome"");
    }
}";

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool DisplayUserMessage(string userText)
    {
        return userText.StartsWith(""Welcome"", StringComparison.CurrentCulture);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_CurrentCulture";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests sorting using directives when adding System.
    /// </summary>
    [Fact]
    public async Task SortUsingDirectivesWhenAddingSystem()
    {
        var test = CreateTest();
        test.TestCode = @"
using System.Text;
using System.Linq;

public class TestClass
{
    public bool CheckText(string text)
    {
        return text.{|XFIX002:StartsWith|}(""prefix"");
    }
}";

        test.FixedCode = @"
using System;
using System.Linq;
using System.Text;

public class TestClass
{
    public bool CheckText(string text)
    {
        return text.StartsWith(""prefix"", StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests handling nested method calls with string comparison.
    /// </summary>
    [Fact]
    public async Task FixNestedStringMethodCalls()
    {
        var test = CreateTest();
        test.TestCode = @"
public class TestClass
{
    public bool ComplexCheck(string text)
    {
        return text.ToUpper().{|XFIX002:StartsWith|}(""PREFIX"");
    }
}";

        test.FixedCode = @"
using System;

public class TestClass
{
    public bool ComplexCheck(string text)
    {
        return text.ToUpper().StartsWith(""PREFIX"", StringComparison.Ordinal);
    }
}";

        test.CodeActionEquivalenceKey = "AddStringComparison_Ordinal";
        await test.RunAsync();
    }
}