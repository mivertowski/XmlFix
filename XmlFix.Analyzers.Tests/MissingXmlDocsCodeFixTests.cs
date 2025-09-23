using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    XmlFix.Analyzers.MissingXmlDocsAnalyzer,
    XmlFix.Analyzers.MissingXmlDocsCodeFix>;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Tests for the MissingXmlDocsCodeFix.
/// </summary>
public class MissingXmlDocsCodeFixTests
{
    /// <summary>
    /// Tests that code fix adds XML documentation to a public class.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_PublicClass()
    {
        var test = @"
public class {|XDOC001:TestClass|}
{
}";

        var fixedCode = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a public method with parameters.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_PublicMethodWithParameters()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public void {|XDOC001:ProcessData|}(string input, int count)
    {
    }
}";

        var fixedCode = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Processes data.
    /// </summary>
    /// <param name=""input"">The input.</param>
    /// <param name=""count"">The count.</param>
    public void ProcessData(string input, int count)
    {
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a public method with return value.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_PublicMethodWithReturnValue()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public string {|XDOC001:GetUserName|}(int userId)
    {
        return ""test"";
    }
}";

        var fixedCode = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Gets the user name.
    /// </summary>
    /// <param name=""userId"">The user identifier.</param>
    /// <returns>The user name.</returns>
    public string GetUserName(int userId)
    {
        return ""test"";
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a public property.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_PublicProperty()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public string {|XDOC001:UserName|} { get; set; }
}";

        var fixedCode = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    /// <value>The user name.</value>
    public string UserName { get; set; }
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a public constructor.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_PublicConstructor()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public {|XDOC001:TestClass|}(string name, int value)
    {
    }
}";

        var fixedCode = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Initializes a new instance of the TestClass class.
    /// </summary>
    /// <param name=""name"">The name.</param>
    /// <param name=""value"">The value.</param>
    public TestClass(string name, int value)
    {
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a public interface.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_PublicInterface()
    {
        var test = @"
public interface {|XDOC001:IUserService|}
{
}";

        var fixedCode = @"
/// <summary>
/// An user service interface.
/// </summary>
public interface IUserService
{
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a public enum.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_PublicEnum()
    {
        var test = @"
public enum {|XDOC001:UserStatus|}
{
}";

        var fixedCode = @"
/// <summary>
/// An user status enumeration.
/// </summary>
public enum UserStatus
{
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a public delegate.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_PublicDelegate()
    {
        var test = @"
public delegate string {|XDOC001:DataProcessor|}(string input, bool validate);";

        var fixedCode = @"
/// <summary>
/// A data processor delegate.
/// </summary>
/// <param name=""input"">The input.</param>
/// <param name=""validate"">A value indicating whether validate.</param>
/// <returns>The result of the operation.</returns>
public delegate string DataProcessor(string input, bool validate);";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a generic class.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_GenericClass()
    {
        var test = @"
public class {|XDOC001:GenericRepository|}<T>
{
}";

        var fixedCode = @"
/// <summary>
/// A generic repository class.
/// </summary>
/// <typeparam name=""T"">The T type parameter.</typeparam>
public class GenericRepository<T>
{
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds XML documentation to a generic method.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_GenericMethod()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public T {|XDOC001:CreateInstance|}<T>() where T : new()
    {
        return new T();
    }
}";

        var fixedCode = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Creates instance.
    /// </summary>
    /// <typeparam name=""T"">The T type parameter.</typeparam>
    /// <returns>The created instance.</returns>
    public T CreateInstance<T>() where T : new()
    {
        return new T();
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests that code fix adds inheritdoc for override methods.
    /// </summary>
    [Fact]
    public async Task AddInheritdoc_OverrideMethod()
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
    public override void {|XDOC001:VirtualMethod|}() { }
}";

        var fixedCode = @"
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
    /// <inheritdoc/>
    public override void VirtualMethod() { }
}";

        await VerifyCS.VerifyCodeFixAsync(test, expected);
    }

    /// <summary>
    /// Tests intelligent summary generation for boolean methods.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_BooleanMethod()
    {
        var test = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public bool {|XDOC001:IsValidUser|}(string userName)
    {
        return true;
    }
}";

        var fixedCode = @"
/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Determines whether valid user.
    /// </summary>
    /// <param name=""userName"">The user name.</param>
    /// <returns>true if the condition is met; otherwise, false.</returns>
    public bool IsValidUser(string userName)
    {
        return true;
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }

    /// <summary>
    /// Tests intelligent summary generation for async methods.
    /// </summary>
    [Fact]
    public async Task AddXmlDocs_AsyncMethod()
    {
        var test = @"
using System.Threading.Tasks;

/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    public async Task<string> {|XDOC001:GetDataAsync|}()
    {
        return await Task.FromResult(""data"");
    }
}";

        var fixedCode = @"
using System.Threading.Tasks;

/// <summary>
/// A test class.
/// </summary>
public class TestClass
{
    /// <summary>
    /// Gets data asynchronously.
    /// </summary>
    /// <returns>The data.</returns>
    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""data"");
    }
}";

        await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
    }
}