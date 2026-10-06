using ST.Api.Logging;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;

using Microsoft.AspNetCore.Authentication;

using System.Security.Claims;
using System.Text.Json;

namespace ST.Api.Middleware;

internal sealed class SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
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

    private string? GetSessionId(HttpContext context)
    {
        // For APIs, only check headers and cookies (not session storage)

        var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault() ?? context.Request.Cookies["session_id"];


        // Only check session storage for non-API requests
        if (sessionId == null && !IsApiRequest(context.Request))
        {
            try
            {
                sessionId = context.Session.GetString("SessionId");
            }
            catch (InvalidOperationException ex)
            {
                MiddlewareLogDefinitions.LogSessionStateUnavailable(logger, ex);
                // Session not configured - ignore for API requests
                // This is expected for API-only applications
            }
        }

        return sessionId;
    }

    private async Task HandleSessionInvalid(HttpContext context, string? reason)
    {
        // Clear any existing authentication
        try
        {
            await context.SignOutAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            MiddlewareLogDefinitions.LogSignOutError(logger, ex);
            throw;
        }

        // Determine if this is an API request or web request
        if (IsApiRequest(context.Request))
        {
            // Return JSON response for API requests
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var apiResponse = new
            {
                success = false,
                message = "Your session has expired or you have been signed in from another location.",
                code = "SESSION_INVALID",
                reason = reason,
                timestamp = DateTimeOffset.UtcNow,
                redirectUrl = "/Auth/SignIn?sessionExpired=true&reason=session_conflict"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(apiResponse)).ConfigureAwait(false);
        }
        else
        {
            // Redirect web requests to login page
            var returnUrl = context.Request.Path + context.Request.QueryString;
            var redirectUrl = $"/Auth/SignIn?sessionExpired=true&reason=session_conflict&returnUrl={Uri.EscapeDataString(returnUrl)}";

            context.Response.Redirect(redirectUrl);
        }
    }

    private static bool IsApiRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
               request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
               request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
    }
}
