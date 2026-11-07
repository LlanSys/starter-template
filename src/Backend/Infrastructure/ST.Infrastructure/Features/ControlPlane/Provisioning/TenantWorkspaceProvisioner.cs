using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

namespace ST.Infrastructure.Features.ControlPlane.Provisioning;

internal sealed class TenantWorkspaceProvisioner(
    IControlPlaneUnitOfWork plane, IServiceScopeFactory scopes, TimeProvider timeProvider) : ITenantWorkspaceProvisioner
{
    public async Task<AppResponse<WorkspaceSetupResponse>> InitializeAsync(
        Guid tenantId, InitializeWorkspaceRequest? request, string actorId, CancellationToken cancellationToken)
    {
        var tenant = await plane.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null || tenant.Status != TenantStatus.Active || !tenant.CanProvision
            || (request is not null && !tenant.CanInviteAdministrator))
        {
            return AppResponses.Failure<WorkspaceSetupResponse>("Complete KYC review and provisioning before setting up the workspace.");
        }

        if (request is not null && tenant.AdministratorEmail is not null
            && !string.Equals(tenant.AdministratorEmail, request.AdministratorEmail.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return AppResponses.Failure<WorkspaceSetupResponse>("Use workspace user management to change its administrator.");
        }

        var stamp = await plane.DeploymentStamps.FindByIdAsync(tenant.DeploymentStampId, cancellationToken).ConfigureAwait(false);
        if (stamp is null)
        {
            return AppResponses.Failure<WorkspaceSetupResponse>("The deployment stamp is unavailable.");
        }

        var modules = await plane.Modules.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var assignments = await plane.Tenants.FirstOrDefaultAsync(
            query => query.Where(item => item.Id == tenantId).Select(item => item.Modules.Select(module => module.ModuleKey).ToArray()),
            cancellationToken).ConfigureAwait(false) ?? [];
        foreach (var module in modules.Where(module => module.IsEssential && !assignments.Contains(module.Key, StringComparer.OrdinalIgnoreCase)))
        {
            await plane.TenantModules.CreateAsync(new TenantModule
            {
                TenantId = tenantId,
                ModuleKey = module.Key,
                CreatedBy = actorId
            }, cancellationToken).ConfigureAwait(false);
        }

        await plane.CompleteAsync(cancellationToken).ConfigureAwait(false);
        await using var scope = scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<WorkspaceTenantOverride>().TenantId = tenantId;
        var policy = await scope.ServiceProvider.GetRequiredService<IEffectiveModulePolicy>().GetAsync(cancellationToken).ConfigureAwait(false);
        if (!policy.Modules.Contains("iam") || !policy.Modules.Contains("core"))
        {
            return AppResponses.Failure<WorkspaceSetupResponse>("Core and Identity must be available before setup.");
        }

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var role = await roles.FindByNameAsync("Tenant Administrator").ConfigureAwait(false);
        if (role is null)
        {
            var roleFactory = scope.ServiceProvider.GetRequiredService<IIamUnitOfWork>();
            var newRole = new AppRole
            {
                Id = Guid.CreateVersion7().ToString(),
                TenantId = tenantId,
                Name = "Tenant Administrator",
                TemplateKey = "iam-administrator",
                TemplateVersion = 1
            };
            await roleFactory.ExecuteInTransactionAsync(async () =>
            {
                Ensure(await roles.CreateAsync(newRole).ConfigureAwait(false));
                foreach (var key in policy.Catalogue
                    .Where(entry => entry.Kind == ModuleCatalogueKind.Permission && policy.EntryAvailable(entry))
                    .Select(entry => entry.Key).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    Ensure(await roles.AddClaimAsync(newRole, new Claim("permission", key)).ConfigureAwait(false));
                }

                await roleFactory.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }

        var now = timeProvider.GetUtcNow();
        tenant.WorkspaceVersion = 1;
        tenant.WorkspaceInitializedAt ??= now;
        tenant.UpdatedBy = actorId;
        if (request is not null)
        {
            tenant.AdministratorEmail ??= request.AdministratorEmail.Trim();
            if (request.SendInvitation && !await plane.AdministratorInvitations.AnyAsync(
                item => item.TenantId == tenantId, cancellationToken).ConfigureAwait(false))
            {
                await plane.AdministratorInvitations.CreateAsync(new TenantAdministratorInvitation(
                    tenantId, tenant.AdministratorEmail, request.FirstName, request.LastName, actorId, now), cancellationToken).ConfigureAwait(false);
            }
        }

        try
        {
            await plane.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AppResponses.Failure<WorkspaceSetupResponse>("The workspace changed. Reload its setup status and try again.");
        }

        return await GetStatusAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<WorkspaceSetupResponse>> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await plane.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return AppResponses.NotFound<WorkspaceSetupResponse>("Workspace not found.");
        }

        var invitation = await plane.AdministratorInvitations.FirstOrDefaultAsync(
            item => item.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
        var accepted = invitation?.State == AdministratorInvitationState.Accepted;
        return AppResponses.Success(new WorkspaceSetupResponse(
            tenantId, tenant.WorkspaceVersion, tenant.AdministratorEmail,
            invitation?.DeliveredAt.HasValue == true, accepted, accepted,
            invitation?.FirstName ?? string.Empty, invitation?.LastName ?? string.Empty, invitation?.State.ToString()));
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Workspace role setup failed.");
        }
    }
}
