using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Threading.Tasks;
using Xunit;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Unit tests for LoggerMessageCodeFix.
/// </summary>
public class LoggerMessageCodeFixTests
{
    /// <summary>
    /// Creates a code fix test for LoggerMessageCodeFix.
    /// </summary>
    /// <returns>The configured code fix test.</returns>
    private static CSharpCodeFixTest<LoggerMessageAnalyzer, LoggerMessageCodeFix, DefaultVerifier> CreateTest()
    {
        var test = new CSharpCodeFixTest<LoggerMessageAnalyzer, LoggerMessageCodeFix, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        // Add mock ILogger interface to avoid version conflicts
        var loggerSource = @"
namespace Microsoft.Extensions.Logging
{
    public interface ILogger
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogTrace(string message, params object[] args);
        void LogCritical(string message, params object[] args);
    }

    public interface ILogger<T> : ILogger { }

    public static class LoggerMessage
    {
        public static System.Action<ILogger, System.Exception?> Define(LogLevel logLevel, EventId eventId, string formatString)
        {
            return (logger, ex) => { };
        }
    }

    public enum LogLevel { Trace, Debug, Information, Warning, Error, Critical, None }
    public struct EventId { public EventId(int id, string? name = null) { } }
}";
        test.TestState.Sources.Add(loggerSource);
        test.FixedState.Sources.Add(loggerSource);

        return test;
    }

    /// <summary>
    /// Tests converting simple logger call with string interpolation to LoggerMessage.
    /// </summary>
    [Fact]
    public async Task ConvertSimpleLoggerCallToLoggerMessage()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void ProcessItem(int id)
    {
        _logger.{|XFIX003:LogInformation|}($""Processing item {id}"");
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, string, Exception?> _informationMessage = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1234), ""Processing item {Id}"");
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void ProcessItem(int id)
    {
        _informationMessage(_logger, id, null);
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting logger call with multiple parameters.
    /// </summary>
    [Fact]
    public async Task ConvertLoggerCallWithMultipleParameters()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void ProcessOrder(int orderId, string customerId)
    {
        _logger.{|XFIX003:LogInformation|}($""Processing order {orderId} for customer {customerId}"");
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, string, string, Exception?> _informationMessage = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(5678), ""Processing order {OrderId} for customer {CustomerId}"");
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void ProcessOrder(int orderId, string customerId)
    {
        _informationMessage(_logger, orderId, customerId, null);
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting logger error call.
    /// </summary>
    [Fact]
    public async Task ConvertLoggerErrorCall()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void HandleError(string operation)
    {
        _logger.{|XFIX003:LogError|}($""Failed to execute {operation}"");
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, string, Exception?> _errorMessage = LoggerMessage.Define<string>(LogLevel.Error, new EventId(9012), ""Failed to execute {Operation}"");
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void HandleError(string operation)
    {
        _errorMessage(_logger, operation, null);
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting logger call with exception parameter.
    /// </summary>
    [Fact]
    public async Task ConvertLoggerCallWithException()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;
using System;

public class TestClass
{
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void HandleError(Exception ex, int requestId)
    {
        _logger.{|XFIX003:LogError|}(ex, $""Failed to process request {requestId}"");
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;
using System;

public class TestClass
{
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, string, Exception> _errorMessage = LoggerMessage.Define<string>(LogLevel.Error, new EventId(3456), ""Failed to process request {RequestId}"");
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void HandleError(Exception ex, int requestId)
    {
        _errorMessage(_logger, requestId, ex);
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests adding field to class with existing fields.
    /// </summary>
    [Fact]
    public async Task AddLoggerMessageFieldToClassWithExistingFields()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    private readonly string _name;
    private readonly ILogger _logger;

    public TestClass(string name, ILogger logger)
    {
        _name = name;
        _logger = logger;
    }

    public void ProcessItem(int id)
    {
        _logger.{|XFIX003:LogInformation|}($""Processing item {id}"");
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    private readonly string _name;
    private readonly ILogger _logger;
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, string, Exception?> _informationMessage = LoggerMessage.Define<string>(LogLevel.Information, new EventId(7890), ""Processing item {Id}"");

    public TestClass(string name, ILogger logger)
    {
        _name = name;
        _logger = logger;
    }

    public void ProcessItem(int id)
    {
        _informationMessage(_logger, id, null);
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting different log levels.
    /// </summary>
    [Fact]
    public async Task ConvertDifferentLogLevels()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void LogTrace(string message)
    {
        _logger.{|XFIX003:LogTrace|}($""Trace: {message}"");
    }

    public void LogWarning(string message)
    {
        _logger.{|XFIX003:LogWarning|}($""Warning: {message}"");
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, string, Exception?> _traceMessage = LoggerMessage.Define<string>(LogLevel.Trace, new EventId(1357), ""Trace: {Message}"");
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void LogTrace(string message)
    {
        _traceMessage(_logger, message, null);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning($""Warning: {message}"");
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests preserving existing using directives.
    /// </summary>
    [Fact]
    public async Task PreserveExistingUsingDirectives()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class TestClass
{
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public async Task ProcessItemAsync(int id)
    {
        await Task.Delay(100);
        _logger.{|XFIX003:LogInformation|}($""Processed item {id}"");
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class TestClass
{
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, string, Exception?> _informationMessage = LoggerMessage.Define<string>(LogLevel.Information, new EventId(2468), ""Processed item {Id}"");
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public async Task ProcessItemAsync(int id)
    {
        await Task.Delay(100);
        _informationMessage(_logger, id, null);
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests converting logger call with string concatenation.
    /// </summary>
    [Fact]
    public async Task ConvertLoggerCallWithStringConcatenation()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void ProcessItem(int id)
    {
        _logger.{|XFIX003:LogError|}(""Failed to process item: "" + id);
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, Exception?> _errorMessage = LoggerMessage.Define(LogLevel.Error, new EventId(1111), ""TODO: Convert message to template"");
    private readonly ILogger _logger;

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void ProcessItem(int id)
    {
        _errorMessage(_logger, null);
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }

    /// <summary>
    /// Tests that field placement is appropriate in classes with methods only.
    /// </summary>
    [Fact]
    public async Task AddFieldToClassWithMethodsOnly()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    private readonly ILogger _logger;

    public void ProcessItem(int id)
    {
        _logger.{|XFIX003:LogInformation|}($""Processing item {id}"");
    }
}";

        test.FixedCode = @"
using Microsoft.Extensions.Logging;

public class TestClass
{
    // LoggerMessage delegate for high-performance logging
    private static readonly Action<ILogger, string, Exception?> _informationMessage = LoggerMessage.Define<string>(LogLevel.Information, new EventId(5555), ""Processing item {Id}"");

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    private readonly ILogger _logger;

    public void ProcessItem(int id)
    {
        _informationMessage(_logger, id, null);
    }
}";

        test.CodeActionEquivalenceKey = "ConvertToLoggerMessage";
        await test.RunAsync();
    }
}