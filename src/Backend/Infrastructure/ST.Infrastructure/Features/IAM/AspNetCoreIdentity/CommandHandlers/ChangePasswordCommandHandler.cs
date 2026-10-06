using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.Application.Utilities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ST.Domain.Shared.Contracts.Common;
using System.Security.Claims;
using ST.Infrastructure.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ChangePasswordCommandHandler(
    UserManager<AppUser> userManager,
    ICurrentActorProvider actorProvider,
    ILogger<ChangePasswordCommandHandler> logger) : IRequestHandler<ChangePasswordCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var userId = actorProvider.ActorId;
            var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);

            if (user is null || !user.IsActive)
            {
                return AppResponses.Failure<bool>("Your account is not active or could not be found.");
            }

            var result = await userManager.ChangePasswordAsync(user, command.Request.CurrentPassword, command.Request.NewPassword).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return AppResponses.Failure<bool>(errors);
            }

            return AppResponses.Success("Password changed successfully.", true);
        }
        catch (Exception ex)
        {
            SecurityLogDefinitions.LogPasswordChangeError(logger, ex);
            throw;
        }
    }
}
