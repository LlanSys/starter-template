using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;
namespace ST.Infrastructure.Features.ControlPlane.Provisioning;

internal sealed class TenantWorkspaceProvisioner(IControlPlaneUnitOfWork plane, IServiceScopeFactory scopes) : ITenantWorkspaceProvisioner
{
    public async Task<AppResponse<WorkspaceSetupResponse>> InitializeAsync(Guid tenantId, InitializeWorkspaceRequest? request, string actorId, CancellationToken ct)
    {
        var tenant = await plane.Tenants.FindByIdAsync(tenantId, ct).ConfigureAwait(false);
        if (tenant is null || tenant.Status != TenantStatus.Active) return AppResponses.Failure<WorkspaceSetupResponse>("Approve and activate the workspace before setting it up.");
        if (request is not null && (tenant.IsDemo || tenant.Id == Guid.Parse("0194f700-0000-7000-8000-000000000001"))) return AppResponses.Failure<WorkspaceSetupResponse>("Administrator onboarding is available for customer workspaces only.");
        var stamp = await plane.DeploymentStamps.FindByIdAsync(tenant.DeploymentStampId, ct).ConfigureAwait(false);
        if (stamp is null) return AppResponses.Failure<WorkspaceSetupResponse>("The deployment stamp is unavailable.");
        if (request is not null && tenant.AdministratorEmail is not null && !string.Equals(tenant.AdministratorEmail, request.AdministratorEmail.Trim(), StringComparison.OrdinalIgnoreCase)) return AppResponses.Failure<WorkspaceSetupResponse>("Use workspace user management to change its administrator.");
        var modules = await plane.Modules.ListAsync(ct: ct).ConfigureAwait(false);
        var assignments = await plane.Tenants.FirstOrDefaultAsync(q => q.Where(x => x.Id == tenantId).Select(x => x.Modules.Select(m => m.ModuleKey).ToArray()), ct).ConfigureAwait(false) ?? [];
        foreach (var module in modules.Where(x => x.IsEssential))
            if (!assignments.Contains(module.Key, StringComparer.OrdinalIgnoreCase)) await plane.TenantModules.CreateAsync(new TenantModule { TenantId = tenantId, ModuleKey = module.Key, CreatedBy = actorId }, ct).ConfigureAwait(false);
        await plane.CompleteAsync(ct).ConfigureAwait(false);
        await using var scope = scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<WorkspaceTenantOverride>().TenantId = tenantId;
        var policy = await scope.ServiceProvider.GetRequiredService<IEffectiveModulePolicy>().GetAsync(ct).ConfigureAwait(false);
        if (!policy.Modules.Contains("iam") || !policy.Modules.Contains("core")) return AppResponses.Failure<WorkspaceSetupResponse>("Core and Identity must be available before setup.");
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var unit = scope.ServiceProvider.GetRequiredService<IIamUnitOfWork>();
        var role = await roles.FindByNameAsync("Tenant Administrator").ConfigureAwait(false);
        await unit.ExecuteInTransactionAsync(async () =>
        {
            if (role is null)
            {
                role = new AppRole { Id = Guid.CreateVersion7().ToString(), TenantId = tenantId, Name = "Tenant Administrator", TemplateKey = "iam-administrator", TemplateVersion = 1 };
                Ensure(await roles.CreateAsync(role).ConfigureAwait(false));
                foreach (var key in policy.Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Permission && policy.EntryAvailable(x)).Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase))
                    Ensure(await roles.AddClaimAsync(role, new Claim("permission", key)).ConfigureAwait(false));
            }
            if (request is not null)
            {
                if (tenant.AdministratorEmail is not null && !string.Equals(tenant.AdministratorEmail, request.AdministratorEmail.Trim(), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The initial administrator cannot be replaced through setup. Manage users in the workspace.");
                var user = tenant.AdministratorUserId is { } id ? await users.FindByIdAsync(id).ConfigureAwait(false) : await users.FindByEmailAsync(request.AdministratorEmail.Trim()).ConfigureAwait(false);
                if (user is null)
                {
                    user = AppUser.CreateAccount(tenantId, request.AdministratorEmail.Trim(), request.FirstName, request.LastName, request.AdministratorEmail.Trim(), null, actorId);
                    user.IsActive = true; user.EmailConfirmed = false; Ensure(await users.CreateAsync(user).ConfigureAwait(false));
                }
                if (!user.IsActive || user.IsDeleted || user.TenantId != tenantId) throw new InvalidOperationException("The workspace administrator account is unavailable.");
                if (!await users.IsInRoleAsync(user, role!.Name!).ConfigureAwait(false)) Ensure(await users.AddToRoleAsync(user, role.Name!).ConfigureAwait(false));
                tenant.AdministratorUserId = user.Id; tenant.AdministratorEmail = user.Email;
            }
            await unit.CompleteAsync(ct).ConfigureAwait(false); return true;
        }, ct).ConfigureAwait(false);
        tenant.WorkspaceVersion = 1; tenant.WorkspaceInitializedAt ??= DateTimeOffset.UtcNow; tenant.UpdatedBy = actorId;
        await plane.CompleteAsync(ct).ConfigureAwait(false);
        if (request?.SendInvitation == true && tenant.AdministratorUserId is { } admin)
        {
            var user = await users.FindByIdAsync(admin).ConfigureAwait(false);
            if (user?.EmailConfirmed == false)
            {
                var sent = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new InviteAdminUserCommand(admin), ct).ConfigureAwait(false);
                if (!sent.IsSuccess) return AppResponses.Failure<WorkspaceSetupResponse>("Workspace is ready. Invitation delivery failed; retry sending the invitation.");
                tenant.InvitationSentAt = DateTimeOffset.UtcNow; await plane.CompleteAsync(ct).ConfigureAwait(false);
            }
        }
        return await GetStatusAsync(tenantId, ct).ConfigureAwait(false);
    }
    public async Task<AppResponse<WorkspaceSetupResponse>> GetStatusAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await plane.Tenants.FindByIdAsync(tenantId, ct).ConfigureAwait(false);
        if (tenant is null) return AppResponses.NotFound<WorkspaceSetupResponse>("Workspace not found.");
        var confirmed = false; var mfa = false; var first = ""; var last = "";
        if (tenant.AdministratorUserId is { } id)
        {
            await using var scope = scopes.CreateAsyncScope(); scope.ServiceProvider.GetRequiredService<WorkspaceTenantOverride>().TenantId = tenantId;
            var user = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByIdAsync(id).ConfigureAwait(false);
            confirmed = user?.EmailConfirmed == true; mfa = user?.TwoFactorEnabled == true; first = user?.FirstName ?? ""; last = user?.LastName ?? "";
        }
        return AppResponses.Success(new WorkspaceSetupResponse(tenantId, tenant.WorkspaceVersion, tenant.AdministratorEmail, tenant.InvitationSentAt.HasValue, confirmed, mfa, first, last));
    }
    private static void Ensure(IdentityResult result) { if (!result.Succeeded) throw new InvalidOperationException("Workspace identity setup failed."); }
}
