using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Demo;
namespace ST.Api.Middleware;

internal sealed class DemoWorkspaceMiddleware(RequestDelegate next, Microsoft.Extensions.Options.IOptions<ST.Infrastructure.Configuration.OrgSettings> settings)
{
    public async Task InvokeAsync(HttpContext context, IamDBContext iam, IControlPlaneUnitOfWork controlPlane)
    {
        var requestedTenant = context.User.FindFirstValue("tenant_id") ?? context.Request.Headers[settings.Value.HeaderName].FirstOrDefault() ?? settings.Value.DefaultTenantId.ToString();
        if (requestedTenant != DemoWorkspace.TenantId.ToString())
        { await next(context).ConfigureAwait(false); return; }
        var actorId = context.User.FindFirstValue("demo_actor_id");
        var isDemo = context.User.HasClaim("demo_workspace", "true");
        var actor = actorId is null ? null : await iam.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(user => user.Id == actorId && user.IsActive && !user.IsDeleted, context.RequestAborted).ConfigureAwait(false);
        var active = await controlPlane.Tenants.AnyAsync(tenant => tenant.Id == DemoWorkspace.TenantId && tenant.IsDemo && tenant.Status == ST.Domain.Features.ControlPlane.Tenants.Enums.TenantStatus.Active, context.RequestAborted).ConfigureAwait(false);
        var staff = actor is not null && await iam.UserClaims.AnyAsync(claim => claim.UserId == actor.Id && claim.ClaimType == PlatformAccess.StaffClaim && claim.ClaimValue == "true", context.RequestAborted).ConfigureAwait(false);
        staff = staff && actor!.TwoFactorEnabled && await iam.UserClaims.AnyAsync(claim => claim.UserId == actor.Id && claim.ClaimType == "permission" && claim.ClaimValue == PlatformAccess.Permission, context.RequestAborted).ConfigureAwait(false);
        if (!isDemo || !active || !staff)
        { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { message = "The demo session is no longer available." }, context.RequestAborted).ConfigureAwait(false); return; }
        var requestPath = context.Request.Path.Value ?? string.Empty;
        if (context.User.HasClaim("demo_readonly", "true") && !HttpMethods.IsGet(context.Request.Method)
            && !requestPath.EndsWith("/logout", StringComparison.OrdinalIgnoreCase) && !requestPath.EndsWith("/refresh-token", StringComparison.OrdinalIgnoreCase))
        { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { message = "This demo session is read-only." }, context.RequestAborted).ConfigureAwait(false); return; }
        // Until sandbox adapters are certified, demo cannot initiate outbound payment or invitation operations.
        var path = context.Request.Path.Value ?? string.Empty;
        if (!HttpMethods.IsGet(context.Request.Method) && (path.Contains("/payments", StringComparison.OrdinalIgnoreCase) || path.Contains("/invite", StringComparison.OrdinalIgnoreCase)))
        { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { message = "This demo does not initiate payments or send invitations." }, context.RequestAborted).ConfigureAwait(false); return; }
        context.Items["TrustedWorkspaceProxy"] = true;
        await next(context).ConfigureAwait(false);
    }
}
