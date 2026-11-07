using ST.Infrastructure.Configuration;
using ST.Api.Logging;
using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace ST.Api.Middleware;

internal sealed class MfaEnrollmentMiddleware(
    RequestDelegate next,
    IOptions<MfaSettings> mfaSettings,
    ILogger<MfaEnrollmentMiddleware> logger)
{
    private readonly MfaSettings _mfaSettings = mfaSettings.Value;

    public async Task InvokeAsync(HttpContext context, UserManager<AppUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userManager);

        if (!await RequiresEnrollmentGateAsync(context, userManager).ConfigureAwait(false))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        MiddlewareLogDefinitions.LogMfaEnrollmentRequired(logger, userId, context.Request.Path.Value ?? string.Empty);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var response = new
        {
            successful = false,
            message = "Two-factor authentication setup is required before you can continue.",
            code = "MFA_ENROLLMENT_REQUIRED",
            redirectUrl = "/iam/security"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response)).ConfigureAwait(false);
    }

    private async Task<bool> RequiresEnrollmentGateAsync(HttpContext context, UserManager<AppUser> userManager)
    {
        if (!_mfaSettings.Enabled || !_mfaSettings.EnforceEnrollment)
        {
            return false;
        }

        if (context.Items.ContainsKey("TrustedWorkspaceProxy") || ShouldSkip(context.Request.Path) || context.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var roleRequiresMfa = context.User.Claims
            .Where(static claim => string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
            .Select(static claim => claim.Value)
            .Any(role => (string.Equals(role, "Tenant Administrator", StringComparison.OrdinalIgnoreCase) || _mfaSettings.RequiredRoles.Contains(role, StringComparer.OrdinalIgnoreCase)));

        if (!roleRequiresMfa)
        {
            return false;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return true;
        }

        return !await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
    }

    private static bool ShouldSkip(PathString path)
    {
        var value = path.Value ?? string.Empty;

        return path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/iam/auth/login", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/iam/auth/refresh-token", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/iam/auth/logout", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/iam/auth/me", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/iam/users/totp", StringComparison.OrdinalIgnoreCase);
    }
}
