using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    XmlFix.Analyzers.MissingXmlDocsAnalyzer,
    XmlFix.Analyzers.MissingXmlDocsCodeFix>;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Comprehensive tests for the MissingXmlDocsCodeFix ensuring 90%+ coverage.
/// </summary>
public class CodeFixTests
{
    /// <summary>
    /// Tests adding documentation to a class.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToClass()
    {
        var test = @"
namespace TestNamespace
{
    public class TestClass { }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// A test class class.
    /// </summary>
    public class TestClass { }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to an interface.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToInterface()
    {
        var test = @"
namespace TestNamespace
{
    public interface ITestInterface { }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// An test interface interface.
    /// </summary>
    public interface ITestInterface { }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to a method with parameters.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToMethodWithParameters()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public void ProcessData(string data, int count) { }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Performs process data.
        /// </summary>
        /// <param name=""data"">The data.</param>
        /// <param name=""count"">The count.</param>
        public void ProcessData(string data, int count) { }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to a method with return value.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToMethodWithReturnValue()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public string GetName() { return """"; }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <returns>The name.</returns>
        public string GetName() { return """"; }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to an async method.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToAsyncMethod()
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
        public async Task<string> GetDataAsync()
        {
            await Task.Delay(100);
            return """";
        }
    }
}";

        var expected = @"
using System.Threading.Tasks;

namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Gets get data async.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public async Task<string> GetDataAsync()
        {
            await Task.Delay(100);
            return """";
        }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to a property.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToProperty()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public string Name { get; set; }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to an event.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToEvent()
    {
        var test = @"
using System;

namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public event EventHandler DataChanged;
    }
}";

        var expected = @"
using System;

namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Occurs when data changed.
        /// </summary>
        public event EventHandler DataChanged;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to a constant field.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToConstField()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public const string DefaultName = ""Default"";
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// The default name.
        /// </summary>
        public const string DefaultName = ""Default"";
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to a constructor.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToConstructor()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public TestClass(string name) { }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Initializes a new instance of the TestClass class.
        /// </summary>
        /// <param name=""name"">The name.</param>
        public TestClass(string name) { }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to a generic method.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToGenericMethod()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public T Process<T>(T input) where T : class
        {
            return input;
        }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Gets process.
        /// </summary>
        /// <typeparam name=""T"">TODO.</typeparam>
        /// <param name=""input"">The input.</param>
        /// <returns>The result of the operation.</returns>
        public T Process<T>(T input) where T : class
        {
            return input;
        }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to an indexer.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToIndexer()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public int this[int index]
        {
            get { return 0; }
            set { }
        }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name=""index"">The index.</param>
        /// <returns>The item at the specified index.</returns>
        public int this[int index]
        {
            get { return 0; }
            set { }
        }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to an operator.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToOperator()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        public static TestClass operator +(TestClass a, TestClass b)
        {
            return new TestClass();
        }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Adds two values.
        /// </summary>
        /// <param name=""a"">The a.</param>
        /// <param name=""b"">The b.</param>
        /// <returns>The result of the operation.</returns>
        public static TestClass operator +(TestClass a, TestClass b)
        {
            return new TestClass();
        }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to a delegate.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToDelegate()
    {
        var test = @"
namespace TestNamespace
{
    public delegate void DataHandler(string data);
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// A data handler delegate.
    /// </summary>
    /// <param name=""data"">The data.</param>
    public delegate void DataHandler(string data);
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation to an enum.
    /// </summary>
    [Fact]
    public async Task AddDocumentationToEnum()
    {
        var test = @"
namespace TestNamespace
{
    public enum Status
    {
        Active,
        Inactive
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// An status enumeration.
    /// </summary>
    public enum Status
    {
        Active,
        Inactive
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests adding documentation preserves existing trivia.
    /// </summary>
    [Fact]
    public async Task PreservesExistingTrivia()
    {
        var test = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        // This is a comment
        [System.Obsolete]
        public void Method() { }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Performs method.
        /// </summary>
        // This is a comment
        [System.Obsolete]
        public void Method() { }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests Fix All in document scenario.
    /// </summary>
    [Fact]
    public async Task FixAllInDocument()
    {
        var test = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void Method1() { }
        public void Method2() { }
        public string Property { get; set; }
    }
}";

        var expected = @"
namespace TestNamespace
{
    /// <summary>
    /// A test class class.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Performs method 1.
        /// </summary>
        public void Method1() { }
        /// <summary>
        /// Performs method 2.
        /// </summary>
        public void Method2() { }
        /// <summary>
        /// Gets or sets the property.
        /// </summary>
        public string Property { get; set; }
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests method name pattern recognition.
    /// </summary>
    [Theory]
    [InlineData("GetData", "Gets the data.")]
    [InlineData("SetValue", "Sets the value.")]
    [InlineData("CreateInstance", "Creates a new instance.")]
    [InlineData("DeleteItem", "Deletes the item.")]
    [InlineData("UpdateRecord", "Updates the record.")]
    [InlineData("FindElement", "Finds the element.")]
    [InlineData("CalculateTotal", "Calculates the total.")]
    [InlineData("IsValid", "Determines whether valid.")]
    [InlineData("HasPermission", "Determines whether permission.")]
    [InlineData("CanExecute", "Determines whether execute.")]
    [InlineData("TryParse", "Attempts to parse.")]
    public async Task MethodNamePatterns(string methodName, string expectedSummary)
    {
        var test = $@"
namespace TestNamespace
{{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {{
        public void {methodName}() {{ }}
    }}
}}";

        var expected = $@"
namespace TestNamespace
{{
    /// <summary>
    /// Test class.
    /// </summary>
    public class TestClass
    {{
        /// <summary>
        /// {expectedSummary}
        /// </summary>
        public void {methodName}() {{ }}
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }
}