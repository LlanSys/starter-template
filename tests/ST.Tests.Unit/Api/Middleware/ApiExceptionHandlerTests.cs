using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ST.Api.Middleware;
using ST.Domain.Shared.Exceptions;

namespace ST.Tests.Unit.Api.Middleware;

public sealed class ApiExceptionHandlerTests
{
    [Theory]
    [InlineData(true, 404)]
    [InlineData(false, 500)]
    public async Task ClientProblemsNeverExposeInternalExceptionDetails(bool unavailableWorkspace, int expectedStatus)
    {
        using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        using var body = new MemoryStream();
        context.Response.Body = body;
        const string privateDiagnostic = "Private diagnostic connection password=fixture-only";
        Exception error = unavailableWorkspace ? new TenantNotResolvedException(privateDiagnostic) : new InvalidOperationException(privateDiagnostic);
        var handler = new ApiExceptionHandler();
        Assert.True(await handler.TryHandleAsync(context, error, CancellationToken.None));
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        body.Position = 0;
        using var reader = new StreamReader(body);
        var text = await reader.ReadToEndAsync();
        Assert.DoesNotContain(privateDiagnostic, text, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", text, StringComparison.OrdinalIgnoreCase);
    }
}
