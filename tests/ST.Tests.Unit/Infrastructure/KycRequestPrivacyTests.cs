using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ST.Infrastructure.Middleware;

namespace ST.Tests.Unit.Infrastructure;

public sealed class KycRequestPrivacyTests
{
    [Theory]
    [InlineData("/api/v1/control-plane/kyc/retention/policies")]
    [InlineData("/api/v1/control-plane/tenants/customer/KYC/documents/document/reviews")]
    public async Task KycRequestBodiesAreNeverWrittenToDebugLogs(string path)
    {
        var logger = new CapturingLogger();
        var nextCalled = false;
        var middleware = new LoggingMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, logger, PathString.Empty);
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.ContentType = "application/json";
        using var body = new MemoryStream(Encoding.UTF8.GetBytes("{\"privateField\":\"synthetic-private-case-note\"}"));
        context.Request.Body = body;
        await middleware.InvokeAsync(context);
        Assert.True(nextCalled);
        Assert.DoesNotContain(Events(logger), text => text.Contains("synthetic-private-case-note", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApprovalEvidenceReferencesAndPrivateReasonsAreRedactedOutsideKycRoutes()
    {
        var logger = new CapturingLogger();
        var middleware = new LoggingMiddleware(_ => Task.CompletedTask, logger, PathString.Empty);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/control-plane/tenants/customer/approve";
        context.Request.ContentType = "application/json";
        using var body = new MemoryStream(Encoding.UTF8.GetBytes("{\"evidenceReference\":\"synthetic-evidence-reference\",\"nested\":{\"reason\":\"synthetic-private-reason\",\"feedback\":\"synthetic-private-feedback\"}}"));
        context.Request.Body = body;
        await middleware.InvokeAsync(context);
        var events = string.Join(" ", Events(logger));
        Assert.DoesNotContain("synthetic-evidence-reference", events, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-private-reason", events, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-private-feedback", events, StringComparison.Ordinal);
        Assert.Contains("approve", events, StringComparison.Ordinal);
    }

    private static IEnumerable<string> Events(CapturingLogger logger)
    {
        return logger.Messages;
    }

    private sealed class CapturingLogger : ILogger<LoggingMiddleware>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
