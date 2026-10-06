using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.HR.Employees.Enums;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateAdminUser(UserManager<AppUser> userManager, ILogger<UpdateAdminUser> logger)
    : IRequestHandler<UpdateAdminUserCommand, AppResponse<AdminUserListResponse>>
{
    public async Task<AppResponse<AdminUserListResponse>> Handle(UpdateAdminUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var req = command.Request;
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null)
            {
                return AppResponses.Failure<AdminUserListResponse>("User not found.");
            }

            var gender = Enum.TryParse<Gender>(req.Gender, true, out var parsedGender) ? parsedGender : Gender.Other;
            user.UpdateAdminProfile(
                req.UserName,
                req.Email,
                req.FirstName,
                req.LastName,
                req.PhoneNumber,
                req.IdNumber,
                gender,
                command.UpdatedBy);

            var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!updateResult.Succeeded)
            {
                return AppResponses.Failure<AdminUserListResponse>(string.Join(", ", updateResult.Errors.Select(static error => error.Description)));
            }

            var currentRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var desiredRoles = req.Roles
                .Where(static role => !string.IsNullOrWhiteSpace(role))
                .Select(static role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rolesToRemove = currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToList();
            var rolesToAdd = desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove).ConfigureAwait(false);
                if (!removeResult.Succeeded)
                {
                    return AppResponses.Failure<AdminUserListResponse>(string.Join(", ", removeResult.Errors.Select(static error => error.Description)));
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await userManager.AddToRolesAsync(user, rolesToAdd).ConfigureAwait(false);
                if (!addResult.Succeeded)
                {
                    return AppResponses.Failure<AdminUserListResponse>(string.Join(", ", addResult.Errors.Select(static error => error.Description)));
                }
            }

            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var response = new AdminUserListResponse(
                user.Id,
                user.UserName ?? string.Empty,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email ?? string.Empty,
                user.PhoneNumber,
                user.IsActive,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.RequirePasswordChange,
                user.CreatedAt,
                user.LastLoginAt,
                user.EmployeeId,
                user.CustomerId,
                [.. roles.Order(StringComparer.OrdinalIgnoreCase)]);

            return AppResponses.Success("User updated.", response);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
