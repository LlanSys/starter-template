using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Api.Features.ControlPlane.Tenants.Dtos;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Application.Features.ControlPlane.Tenants.Queries;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ST.Api.Features.ControlPlane.Tenants.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/tenants")]
[ApiController]
[Authorize]
public sealed class TenantsController(ISender sender) : BaseController
{
    [HttpGet]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> GetAllTenants()
        => HandleResponse(await sender.Send(new GetAllTenantsQuery()).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> GetTenantById(Guid id)
        => HandleResponse(await sender.Send(new GetTenantByIdQuery(id)).ConfigureAwait(false));

    [HttpPost]
    [RequirePermission("control_plane.manage")]
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
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> UpdateTenant(Guid id, UpdateTenantRequest request)
        => HandleResponse(await sender.Send(new UpdateTenantCommand(id, request)).ConfigureAwait(false));

    [HttpPost("{id:guid}/activate")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> ActivateTenant(Guid id)
        => HandleResponse(await sender.Send(new ActivateTenantCommand(id)).ConfigureAwait(false));

    [HttpPost("{id:guid}/suspend")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> SuspendTenant(Guid id)
        => HandleResponse(await sender.Send(new SuspendTenantCommand(id)).ConfigureAwait(false));

    [HttpPost("{id:guid}/approve-kyc")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> ApproveKYC(Guid id)
        => HandleResponse(await sender.Send(new ApproveTenantKYCCommand(id)).ConfigureAwait(false));

    [HttpPost("{id:guid}/modules")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> AddModule(Guid id, AddTenantModuleRequest request)
        => HandleResponse(await sender.Send(new AddTenantModuleCommand(id, request)).ConfigureAwait(false));

    [HttpDelete("{id:guid}/modules/{moduleKey}")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> RemoveModule(Guid id, string moduleKey)
        => HandleResponse(await sender.Send(new RemoveTenantModuleCommand(id, new RemoveTenantModuleRequest { ModuleKey = moduleKey })).ConfigureAwait(false));

    [HttpPost("{id:guid}/stamp-migration")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> MigrateStamp(Guid id, MigrateTenantStampRequest request)
        => HandleResponse(await sender.Send(new MigrateTenantStampCommand(id, request.NewDeploymentStampId, request.NewDatabaseConnectionString)).ConfigureAwait(false));
}
