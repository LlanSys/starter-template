using System.Security.Claims;
using ST.Api.Middleware;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ST.Tests.Unit.Infrastructure.IAM;

public sealed class AccountSessionValidationTests
{
    [Fact]
    public async Task RevokedTokenSessionIsRejectedWithoutHeaderOrCookieSignOutHandler()
    {
        var sessionId = Guid.CreateVersion7().ToString();
        var context = Context(sessionId);
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var service = Substitute.For<ISessionService>();
        service.IsSessionValidAsync(sessionId, "account", Arg.Any<CancellationToken>())
            .Returns(AppResponses.Failure<bool>("Session is not active"));
        var calledNext = false;
        var middleware = new SessionValidationMiddleware(_ => { calledNext = true; return Task.CompletedTask; }, NullLogger<SessionValidationMiddleware>.Instance, TimeProvider.System);
        await middleware.InvokeAsync(context, service);
        Assert.False(calledNext);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task ClientHeaderCannotOverrideRevokedSignedTokenSession()
    {
        var sessionId = Guid.CreateVersion7().ToString();
        var context = Context(sessionId);
        context.Request.Headers["X-Session-Id"] = Guid.CreateVersion7().ToString();
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var service = Substitute.For<ISessionService>();
        service.IsSessionValidAsync(sessionId, "account", Arg.Any<CancellationToken>())
            .Returns(AppResponses.Failure<bool>("Session is not active"));
        var middleware = new SessionValidationMiddleware(_ => Task.CompletedTask, NullLogger<SessionValidationMiddleware>.Instance, TimeProvider.System);
        await middleware.InvokeAsync(context, service);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        await service.Received(1).IsSessionValidAsync(sessionId, "account", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidTokenSessionContinuesProtectedRequest()
    {
        var sessionId = Guid.CreateVersion7().ToString();
        var context = Context(sessionId);
        var service = Substitute.For<ISessionService>();
        service.IsSessionValidAsync(sessionId, "account", Arg.Any<CancellationToken>()).Returns(AppResponses.Success("Valid", true));
        var calledNext = false;
        var middleware = new SessionValidationMiddleware(_ => { calledNext = true; return Task.CompletedTask; }, NullLogger<SessionValidationMiddleware>.Instance, TimeProvider.System);
        await middleware.InvokeAsync(context, service);
        Assert.True(calledNext);
    }

    private static DefaultHttpContext Context(string sessionId)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/iam/admin/users";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "account"), new Claim("session_id", sessionId)], "Bearer"));
        return context;
    }
}