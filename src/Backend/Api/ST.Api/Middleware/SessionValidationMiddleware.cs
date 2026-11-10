using ST.Api.Logging;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;


using System.Security.Claims;
using System.Text.Json;

namespace ST.Api.Middleware;

internal sealed class SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger, TimeProvider timeProvider)
{
    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sessionService);

        // Skip validation for public endpoints
        if (ShouldSkipValidation(context.Request.Path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // Authenticated users must have a session ID
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var sessionId = GetSessionId(context);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
        {
            // If a user is not authenticated or a session ID is missing,
            // we can still allow them to proceed to reach the [Authorize] or [AllowAnonymous] attributes.
            // The framework will handle authorization.
            await next(context).ConfigureAwait(false);
            return;
        }

        // Now, we have an authenticated user with a session ID. Let's validate the session.
        var sessionValidation = await sessionService.IsSessionValidAsync(sessionId, userId, context.RequestAborted).ConfigureAwait(false);
        if (!sessionValidation.IsSuccess)
        {
            MiddlewareLogDefinitions.LogInvalidSession(logger, sessionId, userId, sessionValidation.Message); //

            await HandleSessionInvalid(context, sessionValidation.Message).ConfigureAwait(false);
            return;
        }

        // Session is valid, continue the request pipeline
        await next(context).ConfigureAwait(false);
    }

    private static bool ShouldSkipValidation(PathString path)
    {
        string[] skipPaths =
        [
            // Auth endpoints
            "/Auth/SignIn",
            "/Auth/SignOut",
            "/Auth/Register",
            "/Auth/SessionConfig",
            "/Auth/CheckSession",
            "/Auth/SessionStatus",
            "/Auth/KeepAlive",
            "/ExtendSession",

            // API auth endpoints
            "/api/auth/login",
            "/api/auth/logout",
            "/api/auth/logoutuser",
            "/api/auth/register",
            "/api/auth/refresh-token",

            // Password reset endpoints
            "/Auth/ForgotPassword",
            "/Auth/ResetPassword",
            "/Auth/ConfirmEmail",
            "/api/auth/password/",

            // 2FA endpoints
            "/Auth/TwoFactorLogin",
            "/api/auth/2fa/",

            // Static assets
            "/css/",
            "/js/",
            "/images/",
            "/lib/",
            "/favicon.ico",
            "/_vs/",

            // Health checks and diagnostics
            "/health",
            "/ready",

            // Error pages
            "/Error",
            "/Home/Error"
        ];

        return skipPaths.Any(skipPath =>
            path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetSessionId(HttpContext context)
    {
        return context.User.FindFirstValue("session_id");
    }

    private async Task HandleSessionInvalid(HttpContext context, string? reason)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var response = new
        {
            success = false,
            message = "Your session has expired or you have been signed in from another location.",
            code = "SESSION_INVALID",
            reason,
            timestamp = timeProvider.GetUtcNow(),
            redirectUrl = "/Auth/SignIn?sessionExpired=true&reason=session_conflict"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response), context.RequestAborted).ConfigureAwait(false);
    }
}
