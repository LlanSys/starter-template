using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ST.Infrastructure.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateUserRoles(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    ILogger<UpdateUserRoles> logger)
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

            foreach (var role in requestedRoles)
            {
                if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                {
                    return AppResponses.Failure<UserRolesResponse>($"Role {role} does not exist.");
                }
            }

            var existingRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var rolesToRemove = existingRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToList();
            var rolesToAdd = requestedRoles.Except(existingRoles, StringComparer.OrdinalIgnoreCase).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove).ConfigureAwait(false);
                if (!removeResult.Succeeded)
                {
                    return AppResponses.Failure<UserRolesResponse>(string.Join(", ", removeResult.Errors.Select(static error => error.Description)));
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await userManager.AddToRolesAsync(user, rolesToAdd).ConfigureAwait(false);
                if (!addResult.Succeeded)
                {
                    return AppResponses.Failure<UserRolesResponse>(string.Join(", ", addResult.Errors.Select(static error => error.Description)));
                }
            }

            return AppResponses.Success("User roles updated.", new UserRolesResponse(user.Id, user.UserName ?? user.Email ?? user.Id, requestedRoles));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
