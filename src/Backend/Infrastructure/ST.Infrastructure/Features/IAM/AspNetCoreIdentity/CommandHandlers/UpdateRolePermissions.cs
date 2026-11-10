using System.Security.Claims;
using ST.Application.Features.IAM.Permissions.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Application.Features.IAM.Users;
using ST.Infrastructure.Features.IAM.Authorization;
using ST.Infrastructure.Logging;
using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Modules.Entities;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateRolePermissions(
    RoleManager<AppRole> roleManager,
    IamDBContext context,
    ILogger<UpdateRolePermissions> logger, IEffectiveModulePolicy policy, IamDelegation delegation, IIamUnitOfWork unit)
    : IRequestHandler<UpdateRolePermissionsCommand, AppResponse<RolePermissionsResponse>>
{
    public async Task<AppResponse<RolePermissionsResponse>> Handle(UpdateRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var role = await roleManager.FindByIdAsync(command.RoleId).ConfigureAwait(false);
            if (role is null)
            {
                return AppResponses.Failure<RolePermissionsResponse>("Role not found.");
            }

            if (RoleAccessRules.IsProtectedRole(role.Name))
            {
                return AppResponses.Failure<RolePermissionsResponse>("This role is maintained by workspace setup.");
            }

            var requestedKeys = command.Request.PermissionKeys
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Select(static key => key.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
            if (requestedKeys.Any(key => !access.PermissionAvailable(key))) return AppResponses.Failure<RolePermissionsResponse>("Select permissions from enabled modules; platform permissions cannot be assigned here.");
            if (role.ModuleKey is { } module && (!access.Modules.Contains(module)
                || requestedKeys.Any(key => !string.Equals(access.RegisteredPermissionModule(key), module, StringComparison.OrdinalIgnoreCase))))
            {
                return AppResponses.Failure<RolePermissionsResponse>("Select permissions from this role's enabled module.");
            }

            var validKeys = await context.Permissions
                .AsNoTracking()
                .Where(permission => requestedKeys.Contains(permission.Key) && permission.IsActive)

                .Select(static permission => permission.Key)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            validKeys.AddRange(access.Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Permission && access.EntryAvailable(x)).Select(x => x.Key));
            validKeys.AddRange(EffectiveModuleAccess.TenantSettingsPermissions.Where(access.PermissionAvailable));
            var invalidKeys = requestedKeys
                .Except(validKeys, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (invalidKeys.Count > 0)
            {
                return AppResponses.Failure<RolePermissionsResponse>($"Unknown, inactive, or out-of-scope permission(s): {string.Join(", ", invalidKeys)}.");
            }

            var existingClaims = (await roleManager.GetClaimsAsync(role).ConfigureAwait(false))
                .Where(static claim => claim.Type == "permission")
                .ToList();

            var existingKeys = existingClaims.Select(claim => claim.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var removals = existingClaims.Where(claim => access.PermissionAvailable(claim.Value)
                && !requestedKeys.Contains(claim.Value, StringComparer.OrdinalIgnoreCase)).ToArray();
            var additions = requestedKeys.Where(key => !existingKeys.Contains(key)).ToArray();
            if (!delegation.CanDelegate(additions.Concat(removals.Select(claim => claim.Value))))
            {
                return AppResponses.Failure<RolePermissionsResponse>("You can only change permissions you hold in this workspace.");
            }

            return await unit.ExecuteInTransactionAsync(async () =>
            {
                foreach (var claim in removals)
                {
                    IamGrantUpdateRejectedException.Ensure(await roleManager.RemoveClaimAsync(role, claim).ConfigureAwait(false));
                }

                foreach (var key in additions)
                {
                    IamGrantUpdateRejectedException.Ensure(await roleManager.AddClaimAsync(role, new Claim("permission", key)).ConfigureAwait(false));
                }

                await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return AppResponses.Success("Role permissions updated.",
                    new RolePermissionsResponse(role.Id, role.Name ?? string.Empty, requestedKeys));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (IamGrantUpdateRejectedException)
        {
            return AppResponses.Failure<RolePermissionsResponse>("The requested IAM changes could not be saved.");
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogRoleUpdateError(logger, command.RoleId, ex);
            throw;
        }
    }
}
