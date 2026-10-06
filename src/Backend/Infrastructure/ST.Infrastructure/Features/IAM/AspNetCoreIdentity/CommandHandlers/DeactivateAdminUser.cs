using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class DeactivateAdminUser(UserManager<AppUser> userManager, ILogger<DeactivateAdminUser> logger)
    : IRequestHandler<DeactivateAdminUserCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeactivateAdminUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null)
            {
                return AppResponses.Failure<bool>("User not found.");
            }

            if (!user.IsActive)
            {
                return AppResponses.Success("User is already inactive.", true);
            }

            var reason = string.IsNullOrWhiteSpace(command.Request.Reason)
                ? "Deactivated by administrator"
                : command.Request.Reason.Trim();

            user.RevokeAccess(command.DeactivatedBy, reason);
            var result = await userManager.UpdateAsync(user).ConfigureAwait(false);

            return result.Succeeded
                ? AppResponses.Success("User deactivated.", true)
                : AppResponses.Failure<bool>(string.Join(", ", result.Errors.Select(static error => error.Description)));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogDeactivateUserError(logger, command.UserId, ex);
            throw;
        }
    }
}
