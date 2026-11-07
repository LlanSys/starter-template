using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class CreateAppUser(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IIamUnitOfWork iamUnitOfWork,
    ICurrentTenantProvider tenantProvider,
    ILogger<CreateAppUser> logger, TimeProvider timeProvider) : IRequestHandler<CreateAppUserCommand, AppResponse<AppUserResponse>>
{
    public async Task<AppResponse<AppUserResponse>> Handle(CreateAppUserCommand command, CancellationToken ct)
    {
        var request = command.Request;
        try
        {
            if (tenantProvider.TenantId == Guid.Empty)
            {
                return AppResponses.Failure<AppUserResponse>("A tenant must be resolved before creating an account.");
            }

            var userName = request.Username.Trim();
            var email = request.Email.Trim();
            var normalizedName = userManager.NormalizeName(userName);
            var normalizedEmail = userManager.NormalizeEmail(email);
            // Identity uniqueness spans all tenants and includes soft-deleted accounts.
            var exists = await userManager.Users.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(user => user.NormalizedUserName == normalizedName || user.NormalizedEmail == normalizedEmail, ct)
                .ConfigureAwait(false);
            if (exists)
            {
                return AppResponses.Failure<AppUserResponse>("An account with this username or email already exists.");
            }

            var roles = request.Roles?.Where(static role => !string.IsNullOrWhiteSpace(role))
                .Select(static role => role.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];

            var normalizedRoles = roles.Select(roleManager.NormalizeKey).ToArray();
            if (normalizedRoles.Length > 0 && await roleManager.Roles
                .CountAsync(role => normalizedRoles.Contains(role.NormalizedName), ct).ConfigureAwait(false) != normalizedRoles.Length)
            {
                return AppResponses.Failure<AppUserResponse>("One or more requested roles do not exist.");
            }

            var response = await iamUnitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Begin the IAM transaction before Identity saves the account or role membership.
                var now = timeProvider.GetUtcNow();
                var user = AppUser.CreateAccount(now, tenantProvider.TenantId, userName,
                    request.FirstName, request.LastName, email, request.PhoneNumber, command.UserId);
                var createResult = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
                if (!createResult.Succeeded)
                {
                    throw new AccountCreationRejectedException("The account could not be created. Check the account details and password requirements.");
                }

                if (roles.Length > 0)
                {
                    var roleResult = await userManager.AddToRolesAsync(user, roles).ConfigureAwait(false);
                    if (!roleResult.Succeeded)
                    {
                        throw new AccountCreationRejectedException("The requested roles could not be assigned.");
                    }
                }

                var profile = AppUserProfile.Create(now, user.Id, user.PhoneNumber, user.PhoneNumber, user.Email, command.UserId);
                await iamUnitOfWork.AppUserProfileRepository.CreateAsync(profile, ct).ConfigureAwait(false);
                await iamUnitOfWork.CompleteAsync(ct).ConfigureAwait(false);
                // The transactional UoW dispatches this event after the operation, before committing.
                user.RaiseAppUserCreatedEvent(now);
                return AppResponses.Success("User created successfully.", user.ToAppUserResponse(roles));
            }, ct).ConfigureAwait(false);

            ServiceLogDefinitions.LogAppUserCreated(logger, response.Data!.Id, email);
            return response;
        }
        catch (AccountCreationRejectedException ex)
        {
            // A rejection after Identity writes must roll back, then become an expected response.
            ServiceLogDefinitions.LogAppUserCreationWarning(logger, request.Email, ex.Message);
            return AppResponses.Failure<AppUserResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogAppUserCreationFailed(logger, request.Email, ex);
            throw;
        }
    }

    private sealed class AccountCreationRejectedException(string message) : Exception(message) { }
}
