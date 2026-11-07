using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using ST.Domain.Features.IAM.Users.Entities;

namespace ST.Api.Middleware;

internal sealed class AdministratorSetupMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserManager<AppUser> users)
    {
        var principal = context.User;
        if (principal.HasClaim("temp_auth", "true"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (principal.FindFirstValue("session_id") is not null
            && principal.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId
            && !IsSetupRoute(context)
            && (await users.FindByIdAsync(userId).ConfigureAwait(false))?.RequiresAdministratorSetup == true)
        {
            await Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                title: "Finish administrator setup before accessing the workspace.").ExecuteAsync(context).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsSetupRoute(HttpContext context)
    {
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        return (action?.ControllerName, action?.ActionName) switch
        {
            ("Auth", "GetCurrentUser" or "Logout" or "RefreshToken") => true,
            ("Totp", "InitiateSetup" or "GetStatus" or "Verify") => true,
            ("AdministratorInvitation", "Complete") => true,
            _ => false
        };
    }
}
