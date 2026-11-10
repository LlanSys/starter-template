using System.Security.Claims;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infrastructure.Features.IAM.Authorization;
using ST.Infrastructure.Logging;
using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Modules.Entities;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateUserPermissions(
    UserManager<AppUser> userManager,
    IamDBContext context,
    ILogger<UpdateUserPermissions> logger, IEffectiveModulePolicy policy, IamDelegation delegation, IIamUnitOfWork unit)
    : IRequestHandler<UpdateUserPermissionsCommand, AppResponse<UserPermissionsResponse>>
{
    public async Task<AppResponse<UserPermissionsResponse>> Handle(UpdateUserPermissionsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null)
            {
                return AppResponses.Failure<UserPermissionsResponse>("User not found.");
            }

            var requestedKeys = command.Request.PermissionKeys
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Select(static key => key.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
            if (requestedKeys.Any(key => !access.PermissionAvailable(key))) return AppResponses.Failure<UserPermissionsResponse>("Select permissions from enabled modules; platform permissions cannot be assigned here.");
            var validKeys = await context.Permissions
                .AsNoTracking()
                .Where(permission => requestedKeys.Contains(permission.Key) && permission.IsActive)
                .Select(static permission => permission.Key)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            validKeys.AddRange(access.Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Permission && access.EntryAvailable(x)).Select(x => x.Key));
            validKeys.AddRange(EffectiveModuleAccess.TenantSettingsPermissions.Where(access.PermissionAvailable));
            var invalidKeys = requestedKeys.Except(validKeys, StringComparer.OrdinalIgnoreCase).ToList();
            if (invalidKeys.Count > 0)
            {
                return AppResponses.Failure<UserPermissionsResponse>($"Unknown or inactive permission(s): {string.Join(", ", invalidKeys)}.");
            }

            var existingClaims = (await userManager.GetClaimsAsync(user).ConfigureAwait(false))
                .Where(static claim => claim.Type == "permission")
                .ToList();

            var existingKeys = existingClaims.Select(claim => claim.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var removals = existingClaims.Where(claim => access.PermissionAvailable(claim.Value)
                && !requestedKeys.Contains(claim.Value, StringComparer.OrdinalIgnoreCase)).ToArray();
            var additions = requestedKeys.Where(key => !existingKeys.Contains(key)).ToArray();
            if (!delegation.CanDelegate(additions.Concat(removals.Select(claim => claim.Value))))
            {
                return AppResponses.Failure<UserPermissionsResponse>("You can only change permissions you hold in this workspace.");
            }

            return await unit.ExecuteInTransactionAsync(async () =>
            {
                if (removals.Length > 0)
                {
                    IamGrantUpdateRejectedException.Ensure(await userManager.RemoveClaimsAsync(user, removals).ConfigureAwait(false));
                }

                if (additions.Length > 0)
                {
                    var claims = additions.Select(key => new Claim("permission", key));
                    IamGrantUpdateRejectedException.Ensure(await userManager.AddClaimsAsync(user, claims).ConfigureAwait(false));
                }

                await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return AppResponses.Success("User permissions updated.",
                    new UserPermissionsResponse(user.Id, user.UserName ?? user.Email ?? user.Id, requestedKeys));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (IamGrantUpdateRejectedException)
        {
            return AppResponses.Failure<UserPermissionsResponse>("The requested IAM changes could not be saved.");
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
