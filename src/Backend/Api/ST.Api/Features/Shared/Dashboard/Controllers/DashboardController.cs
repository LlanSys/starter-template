using ST.Domain.Features.ControlPlane.Tenants.Entities;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.Shared.Dashboard;
using ST.SharedKernel.Features.ControlPlane;

namespace ST.Api.Features.Shared.Dashboard.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
[RequireModule("core", "/dashboard")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DashboardController(ISender sender, IAuthorizationService authorization) : BaseController
{
    [HttpGet("attention")]
    public async Task<IActionResult> Attention(CancellationToken cancellationToken)
    {
        // Each category uses the same native permission policy as its destination API.
        // No actor, tenant, role name or permission selector is accepted from the caller.
        var viewKyc = await CanAsync(PlatformPermissions.ViewKyc).ConfigureAwait(false);
        var kyc = viewKyc && await CanAsync(PlatformPermissions.ApproveKyc).ConfigureAwait(false);
        var tenants = await CanAsync(PlatformPermissions.ViewTenants).ConfigureAwait(false);
        var provisioning = tenants && await CanAsync(PlatformPermissions.ManageTenantLifecycle).ConfigureAwait(false);
        var invitations = tenants && await CanAsync(PlatformPermissions.ManageTenantAdministrators).ConfigureAwait(false);
        var holds = viewKyc && await CanAsync(PlatformPermissions.ManageKycHolds).ConfigureAwait(false);
        var support = await CanAsync("support.approve").ConfigureAwait(false);
        var approvedSupport = await CanAsync(PlatformPermissions.ViewSupport).ConfigureAwait(false)
            && await CanAsync(PlatformPermissions.ManageSupport).ConfigureAwait(false);

        return HandleResponse(await sender.Send(new GetDashboardAttentionQuery(
            new DashboardAttentionAccess(kyc, provisioning, invitations, holds, support, approvedSupport)),
            cancellationToken).ConfigureAwait(false));
    }

    private async Task<bool> CanAsync(string permission)
    {
        if (permission.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) && (!PlatformAccess.HasMfa(User)
                || !Guid.TryParse(User.FindFirst("tenant_id")?.Value, out var owner) || owner != Tenant.PlatformTenantId))
        {
            return false;
        }

        var result = await authorization.AuthorizeAsync(User, PermissionAuthorizationConstants.BuildPolicyName(permission))
            .ConfigureAwait(false);

        return result.Succeeded;
    }
}
