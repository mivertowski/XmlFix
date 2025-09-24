using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Threading.Tasks;
using Xunit;
using XmlFix.Analyzers.Common;

namespace XmlFix.Analyzers.Tests;

/// <summary>
/// Unit tests for LoggerMessageAnalyzer.
/// </summary>
public class LoggerMessageAnalyzerTests
{
    /// <summary>
    /// Creates an analyzer test for LoggerMessageAnalyzer.
    /// </summary>
    /// <returns>The configured analyzer test.</returns>
    private static CSharpAnalyzerTest<LoggerMessageAnalyzer, DefaultVerifier> CreateTest()
    {
        var test = new CSharpAnalyzerTest<LoggerMessageAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        };

        // Add mock ILogger interface to avoid version conflicts
        test.TestState.Sources.Add(@"
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

        void LogInformation(System.Exception? exception, string message, params object[] args);
        void LogWarning(System.Exception? exception, string message, params object[] args);
        void LogError(System.Exception? exception, string message, params object[] args);
        void LogDebug(System.Exception? exception, string message, params object[] args);
        void LogTrace(System.Exception? exception, string message, params object[] args);
        void LogCritical(System.Exception? exception, string message, params object[] args);
    }

    public interface ILogger<T> : ILogger { }

    public static class LoggerMessage
    {
        public static System.Action<ILogger, System.Exception?> Define(LogLevel logLevel, EventId eventId, string formatString)
        {
            return (logger, ex) => { };
        }

        public static System.Action<ILogger, T, System.Exception?> Define<T>(LogLevel logLevel, EventId eventId, string formatString)
        {
            return (logger, param, ex) => { };
        }

        public static System.Action<ILogger, T1, T2, System.Exception?> Define<T1, T2>(LogLevel logLevel, EventId eventId, string formatString)
        {
            return (logger, param1, param2, ex) => { };
        }

        public static System.Action<ILogger, T1, T2, T3, System.Exception?> Define<T1, T2, T3>(LogLevel logLevel, EventId eventId, string formatString)
        {
            return (logger, param1, param2, param3, ex) => { };
        }
    }

    public enum LogLevel { Trace, Debug, Information, Warning, Error, Critical, None }
    public struct EventId { public EventId(int id, string? name = null) { } }
}");

        return test;
    }

    /// <summary>
    /// Tests that logger calls with string interpolation trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerWithStringInterpolation_ShouldTriggerDiagnostic()
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

    public void ProcessItem(int id, string name)
    {
        _logger.{|XFIX003:LogInformation|}($""Processing item {id} with name {name}"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls with string concatenation trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerWithStringConcatenation_ShouldTriggerDiagnostic()
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

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls in loops trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerInLoop_ShouldTriggerDiagnostic()
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

    public void ProcessItems(int[] items)
    {
        foreach (var item in items)
        {
            _logger.{|XFIX003:LogDebug|}($""Processing item {item}"");
        }
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that simple string literal logger calls do not trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerWithSimpleString_ShouldNotTriggerDiagnostic()
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

    public void LogMessage()
    {
        _logger.LogInformation(""Simple message"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls with LoggerMessage.Define do not trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerWithLoggerMessage_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;
using System;

public class TestClass
{
    private readonly ILogger _logger;
    private static readonly Action<ILogger, int, Exception?> ProcessingItem =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), ""Processing item {Id}"");

    public TestClass(ILogger logger)
    {
        _logger = logger;
    }

    public void ProcessItem(int id)
    {
        ProcessingItem(_logger, id, null);
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls in async methods trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerInAsyncMethod_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;
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

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls with multiple parameters trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerWithMultipleParameters_ShouldTriggerDiagnostic()
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

    public void ProcessOrder(int orderId, string customerId, decimal amount)
    {
        _logger.{|XFIX003:LogInformation|}($""Processing order {orderId} for customer {customerId} with amount {amount}"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls in performance-critical classes trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerInPerformanceCriticalClass_ShouldTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class OrderProcessor
{
    private readonly ILogger _logger;

    public OrderProcessor(ILogger logger)
    {
        _logger = logger;
    }

    public void ProcessOrder(int orderId)
    {
        _logger.{|XFIX003:LogInformation|}($""Processing order {orderId}"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that different logger methods trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task DifferentLoggerMethods_ShouldTriggerDiagnostics()
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

    public void LogMessages(string message)
    {
        _logger.{|XFIX003:LogTrace|}($""Trace: {message}"");
        _logger.{|XFIX003:LogDebug|}($""Debug: {message}"");
        _logger.{|XFIX003:LogInformation|}($""Info: {message}"");
        _logger.{|XFIX003:LogWarning|}($""Warning: {message}"");
        _logger.{|XFIX003:LogError|}($""Error: {message}"");
        _logger.{|XFIX003:LogCritical|}($""Critical: {message}"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls with exceptions trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerWithException_ShouldTriggerDiagnostic()
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

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls with string.Format trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerWithStringFormat_ShouldTriggerDiagnostic()
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

    public void ProcessItem(int id, string name)
    {
        _logger.{|XFIX003:LogInformation|}(string.Format(""Processing item {0} with name {1}"", id, name));
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that non-ILogger types do not trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task NonILoggerType_ShouldNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
public class CustomLogger
{
    public void LogInformation(string message) { }
}

public class TestClass
{
    private readonly CustomLogger _logger;

    public TestClass(CustomLogger logger)
    {
        _logger = logger;
    }

    public void LogMessage(int id)
    {
        _logger.LogInformation($""Message {id}"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls outside performance-critical contexts might not trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerInNonCriticalContext_MightNotTriggerDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = @"
using Microsoft.Extensions.Logging;

public class SimpleClass
{
    private readonly ILogger _logger;

    public SimpleClass(ILogger logger)
    {
        _logger = logger;
    }

    public void SimpleMethod(int id)
    {
        _logger.{|XFIX003:LogInformation|}($""Simple message {id}"");
    }
}";

        await test.RunAsync();
    }

    /// <summary>
    /// Tests that logger calls with expensive operations trigger diagnostic.
    /// </summary>
    [Fact]
    public async Task LoggerWithExpensiveOperations_ShouldTriggerDiagnostic()
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

    public void ProcessData(object data)
    {
        _logger.{|XFIX003:LogInformation|}($""Processing data: {data.ToString().ToUpper()}"");
    }
}";

        await test.RunAsync();
    }
}