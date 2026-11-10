using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.IAM.Users;
using ST.Infrastructure.Features.IAM.Authorization;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
namespace ST.Infrastructure.Features.ControlPlane.Modules;
internal sealed class ApplyRoleTemplateHandler(IEffectiveModulePolicy policy, RoleManager<AppRole> roles, IIamUnitOfWork unit, ICurrentTenantProvider tenant, IamDelegation delegation) : IRequestHandler<ApplyRoleTemplateCommand, AppResponse<AdminRoleListResponse>>
{
    public async Task<AppResponse<AdminRoleListResponse>> Handle(ApplyRoleTemplateCommand command, CancellationToken cancellationToken)
    {
        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        var template = access.Catalogue.FirstOrDefault(x => x.Key == command.Request.TemplateKey && x.Kind == ModuleCatalogueKind.RoleTemplate && access.EntryAvailable(x));
        if (template is null || template.PermissionKeys.Any(key => !access.PermissionAvailable(key))) return AppResponses.Failure<AdminRoleListResponse>("This role template is not available for your tenant.");
        if (template.Version != command.Request.ExpectedVersion) return AppResponses.Failure<AdminRoleListResponse>("The template changed. Reload it before applying.");
        var name = command.Request.RoleName.Trim();
        if (RoleAccessRules.IsProtectedRole(name) || !delegation.CanDelegate(template.PermissionKeys))
        {
            return AppResponses.Failure<AdminRoleListResponse>("This role template or name cannot be assigned by this account.");
        }

        var existing = await roles.FindByNameAsync(name).ConfigureAwait(false);
        if (existing is not null && existing.TemplateKey != template.Key) return AppResponses.Failure<AdminRoleListResponse>("This name belongs to another tenant role. Choose a different name.");
        return await unit.ExecuteInTransactionAsync(async () =>
        {
            var role = existing ?? new AppRole { Id = Guid.CreateVersion7().ToString(), TenantId = tenant.TenantId, Name = name, ModuleKey = template.ModuleKey, TemplateKey = template.Key };
            if (existing is null && !(await roles.CreateAsync(role).ConfigureAwait(false)).Succeeded) throw new InvalidOperationException("Could not create the role from its template.");
            var current = await roles.GetClaimsAsync(role).ConfigureAwait(false);
            foreach (var permission in template.PermissionKeys.Where(key => !current.Any(claim => claim.Type == "permission" && claim.Value == key)))
                if (!(await roles.AddClaimAsync(role, new Claim("permission", permission)).ConfigureAwait(false)).Succeeded) throw new InvalidOperationException("Could not apply the role template.");
            role.TemplateVersion = template.Version;
            if (!(await roles.UpdateAsync(role).ConfigureAwait(false)).Succeeded) throw new InvalidOperationException("Could not record the role template revision.");
            await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return AppResponses.Success(new AdminRoleListResponse(role.Id, role.Name!, role.NormalizedName!, 0));
        }, cancellationToken).ConfigureAwait(false);
    }
}
