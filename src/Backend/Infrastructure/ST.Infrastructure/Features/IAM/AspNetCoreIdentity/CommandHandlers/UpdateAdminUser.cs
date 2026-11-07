using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateAdminUser(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IIamUnitOfWork iamUnitOfWork,
    ICurrentTenantProvider tenantProvider,
    ILogger<UpdateAdminUser> logger, TimeProvider timeProvider) : IRequestHandler<UpdateAdminUserCommand, AppResponse<AdminUserListResponse>>
{
    public async Task<AppResponse<AdminUserListResponse>> Handle(UpdateAdminUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null || user.IsDeleted || tenantProvider.TenantId == Guid.Empty || user.TenantId != tenantProvider.TenantId)
            {
                return AppResponses.NotFound<AdminUserListResponse>("User not found.");
            }

            var requestedRoles = command.Request.Roles.Where(static role => !string.IsNullOrWhiteSpace(role))
                .Select(static role => role.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var normalizedRoles = requestedRoles.Select(roleManager.NormalizeKey).ToArray();
            if (normalizedRoles.Length > 0 && await roleManager.Roles
                .CountAsync(role => normalizedRoles.Contains(role.NormalizedName), cancellationToken).ConfigureAwait(false) != normalizedRoles.Length)
            {
                return AppResponses.Failure<AdminUserListResponse>("One or more requested roles do not exist.");
            }

            return await iamUnitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var request = command.Request;
                var now = timeProvider.GetUtcNow();
                user.UpdateAdminProfile(now, request.UserName, request.Email, request.FirstName, request.LastName,
                    request.PhoneNumber, command.UpdatedBy);
                EnsureSucceeded(await userManager.UpdateAsync(user).ConfigureAwait(false), "The user profile could not be updated. Check the account details.");

                var currentRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
                var desiredRoles = requestedRoles;
                var rolesToRemove = currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToArray();
                var rolesToAdd = desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
                if (rolesToRemove.Length > 0)
                {
                    EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, rolesToRemove).ConfigureAwait(false), "The requested role changes could not be saved.");
                }
                if (rolesToAdd.Length > 0)
                {
                    EnsureSucceeded(await userManager.AddToRolesAsync(user, rolesToAdd).ConfigureAwait(false), "The requested role changes could not be saved.");
                }

                var profile = AppUserProfile.Create(now, user.Id, user.PhoneNumber, user.PhoneNumber, user.Email, command.UpdatedBy);
                await iamUnitOfWork.AppUserProfileRepository.CreateOrUpdateAsync(user.Id, profile, cancellationToken).ConfigureAwait(false);
                await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
                return AppResponses.Success("User updated.", new AdminUserListResponse(
                    user.Id, user.UserName ?? string.Empty, $"{user.FirstName} {user.LastName}".Trim(),
                    user.Email ?? string.Empty, user.PhoneNumber, user.IsActive, user.EmailConfirmed,
                    user.TwoFactorEnabled, user.RequirePasswordChange, user.CreatedAt, user.LastLoginAt,
                    [.. roles.Order(StringComparer.OrdinalIgnoreCase)]));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (AccountUpdateRejectedException ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            return AppResponses.Failure<AdminUserListResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUpdateUserError(logger, command.UserId, ex);
            throw;
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new AccountUpdateRejectedException(message);
        }
    }

    private sealed class AccountUpdateRejectedException(string message) : Exception(message) { }
}
