using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ST.Infrastructure.Features.IAM.Authorization;
using ST.Infrastructure.Logging;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateUserRoles(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    ILogger<UpdateUserRoles> logger, IRoleAvailability availability, IamDelegation delegation, IIamUnitOfWork unit)
    : IRequestHandler<UpdateUserRolesCommand, AppResponse<UserRolesResponse>>
{
    public async Task<AppResponse<UserRolesResponse>> Handle(UpdateUserRolesCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null)
            {
                return AppResponses.Failure<UserRolesResponse>("User not found.");
            }

            var requestedRoles = command.Request.Roles
                .Where(static role => !string.IsNullOrWhiteSpace(role))
                .Select(static role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var allowed = await availability.AvailableIdsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var role in requestedRoles)
            {
                var registered = await roleManager.FindByNameAsync(role).ConfigureAwait(false);
                if (registered is null || !allowed.Contains(registered.Id))
                {
                    return AppResponses.Failure<UserRolesResponse>($"Role {role} does not exist.");
                }
            }

            var existingRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var rolesToRemove = new List<string>();
            foreach (var existing in existingRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase))
            {
                var registered = await roleManager.FindByNameAsync(existing).ConfigureAwait(false);
                if (registered is not null && allowed.Contains(registered.Id)) rolesToRemove.Add(existing);
            }
            var rolesToAdd = requestedRoles.Except(existingRoles, StringComparer.OrdinalIgnoreCase).ToList();

            var effectiveRoles = existingRoles.Except(rolesToRemove, StringComparer.OrdinalIgnoreCase).Concat(rolesToAdd);
            if (!await delegation.CanChangeRolesAsync(existingRoles, effectiveRoles, cancellationToken).ConfigureAwait(false))
            {
                return AppResponses.Failure<UserRolesResponse>("You cannot change one or more of these role assignments.");
            }

            return await unit.ExecuteInTransactionAsync(async () =>
            {
                if (rolesToRemove.Count > 0)
                {
                    IamGrantUpdateRejectedException.Ensure(await userManager.RemoveFromRolesAsync(user, rolesToRemove).ConfigureAwait(false));
                }

                if (rolesToAdd.Count > 0)
                {
                    IamGrantUpdateRejectedException.Ensure(await userManager.AddToRolesAsync(user, rolesToAdd).ConfigureAwait(false));
                }

                await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return AppResponses.Success("User roles updated.",
                    new UserRolesResponse(user.Id, user.UserName ?? user.Email ?? user.Id, effectiveRoles.ToList()));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (IamGrantUpdateRejectedException)
        {
            return AppResponses.Failure<UserRolesResponse>("The requested IAM changes could not be saved.");
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
