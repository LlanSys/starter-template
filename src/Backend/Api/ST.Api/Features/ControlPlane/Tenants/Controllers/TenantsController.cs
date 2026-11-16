using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Api.Features.ControlPlane.Tenants.Dtos;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Application.Features.ControlPlane.Tenants.Queries;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;
using ST.SharedKernel.Features.ControlPlane;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Shared.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ST.Api.Features.ControlPlane.Tenants.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/tenants")]
[ApiController]
[Authorize]
public sealed class TenantsController(ISender sender) : BaseController
{
    [HttpGet("{id:guid}/frontend-address")]
    [RequirePermission(PlatformPermissions.ViewTenants)]
    public async Task<IActionResult> GetFrontendAddress(Guid id, CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new GetTenantFrontendAddressQuery(id), ct).ConfigureAwait(false));
    }

    [HttpPut("{id:guid}/frontend-address")]
    [RequirePermission(PlatformPermissions.EditTenants)]
    public async Task<IActionResult> SaveFrontendAddress(Guid id, SaveTenantFrontendAddressRequest request, CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new SaveTenantFrontendAddressCommand(id, request), ct).ConfigureAwait(false));
    }

    [HttpGet]
    [RequirePermission(PlatformPermissions.ViewTenants)]
    public async Task<IActionResult> GetAllTenants()
    {
        return HandleResponse(await sender.Send(new GetAllTenantsQuery()).ConfigureAwait(false));
    }

    [HttpGet("dashboard")]
    [RequirePermission(PlatformPermissions.ViewTenants)]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(
            new GetPlatformDashboardQuery(PlatformAccess.Can(User, PlatformPermissions.ManageTenantAdministrators)),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/workspace")]
    [RequirePermission(PlatformPermissions.ManageTenantAdministrators)]
    public async Task<IActionResult> InitializeWorkspace(Guid id, InitializeWorkspaceRequest request, [FromServices] ICurrentActorProvider actor, CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new InitializeWorkspaceCommand(id, request, actor.ActorId), ct).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/workspace/invitation/resend")]
    [RequirePermission(PlatformPermissions.ManageTenantAdministrators)]
    public async Task<IActionResult> ResendInvitation(Guid id, [FromServices] IAdministratorInvitationService invitations, CancellationToken cancellationToken)
    {
        return HandleResponse(await invitations.ChangeDeliveryAsync(id, false, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/workspace/invitation/revoke")]
    [RequirePermission(PlatformPermissions.ManageTenantAdministrators)]
    public async Task<IActionResult> RevokeInvitation(Guid id, [FromServices] IAdministratorInvitationService invitations, CancellationToken cancellationToken)
    {
        return HandleResponse(await invitations.ChangeDeliveryAsync(id, true, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{id:guid}/workspace")]
    [RequirePermission(PlatformPermissions.ManageTenantAdministrators)]
    public async Task<IActionResult> WorkspaceStatus(Guid id, [FromServices] ITenantWorkspaceProvisioner provisioner, CancellationToken ct)
    {
        return HandleResponse(await provisioner.GetStatusAsync(id, ct).ConfigureAwait(false));
    }

    [HttpGet("{id:guid}/workspace/tenant")]
    [RequirePermission(PlatformPermissions.ManageTenantAdministrators)]
    public async Task<IActionResult> WorkspaceTenant(Guid id, CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new GetTenantByIdQuery(id), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PlatformPermissions.ViewTenants)]
    public async Task<IActionResult> GetTenantById(Guid id)
    {
        return HandleResponse(await sender.Send(new GetTenantByIdQuery(id)).ConfigureAwait(false));
    }

    [HttpPost]
    [RequirePermission(PlatformPermissions.CreateTenants)]
    public async Task<IActionResult> CreateTenant(CreateTenantRequest request)
    {
        var response = await sender.Send(new CreateTenantCommand(request)).ConfigureAwait(false);
        return HandleResponse(response, onSuccess: r =>
        {
            if (string.Equals(r.Data?.Status, "Provisioning", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(r);
            }

            return Ok(r);
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PlatformPermissions.EditTenants)]
    public async Task<IActionResult> UpdateTenant(Guid id, UpdateTenantRequest request)
    {
        return HandleResponse(await sender.Send(new UpdateTenantCommand(id, request)).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(PlatformPermissions.ManageTenantLifecycle)]
    public async Task<IActionResult> ActivateTenant(Guid id)
    {
        return HandleResponse(await sender.Send(new ActivateTenantCommand(id)).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/suspend")]
    [RequirePermission(PlatformPermissions.ManageTenantLifecycle)]
    public async Task<IActionResult> SuspendTenant(Guid id)
    {
        return HandleResponse(await sender.Send(new SuspendTenantCommand(id)).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(PlatformPermissions.ApproveKyc)]
    public async Task<IActionResult> Approve(Guid id, ApproveTenantRequest request, CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new ApproveTenantCommand(id, request.EvidenceReference), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/provisioning/retry")]
    [RequirePermission(PlatformPermissions.ManageTenantLifecycle)]
    public async Task<IActionResult> RetryProvisioning(Guid id, CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new ApproveTenantCommand(id, null, Retry: true), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/modules")]
    [RequirePermission(PlatformPermissions.ManageTenantModules)]
    public async Task<IActionResult> AddModule(Guid id, AddTenantModuleRequest request)
    {
        return HandleResponse(await sender.Send(new AddTenantModuleCommand(id, request)).ConfigureAwait(false));
    }

    [HttpDelete("{id:guid}/modules/{moduleKey}")]
    [RequirePermission(PlatformPermissions.ManageTenantModules)]
    public async Task<IActionResult> RemoveModule(Guid id, string moduleKey)
    {
        return HandleResponse(await sender.Send(new RemoveTenantModuleCommand(id, new RemoveTenantModuleRequest { ModuleKey = moduleKey })).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/stamp-migration")]
    [RequirePermission(PlatformPermissions.MigrateTenants)]
    public async Task<IActionResult> MigrateStamp(Guid id, MigrateTenantStampRequest request)
    {
        return HandleResponse(await sender.Send(new MigrateTenantStampCommand(id, request.NewDeploymentStampId, request.NewDatabaseConnectionString, request.NewClientOrigin)).ConfigureAwait(false));
    }
}
