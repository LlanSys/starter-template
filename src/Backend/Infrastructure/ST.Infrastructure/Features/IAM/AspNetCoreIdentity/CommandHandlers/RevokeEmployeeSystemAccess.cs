using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RevokeEmployeeSystemAccess(
    UserManager<AppUser> userManager,
    IIamUnitOfWork iamUnitOfWork,
    ISessionService sessionService,
    ILogger<RevokeEmployeeSystemAccess> logger)
    : IRequestHandler<RevokeEmployeeSystemAccessCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(RevokeEmployeeSystemAccessCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.Users
                .SingleOrDefaultAsync(item => item.EmployeeId == command.EmployeeId, cancellationToken)
                .ConfigureAwait(false);

            if (user is null)
            {
                return AppResponses.Failure<bool>("This employee is not linked to an IAM user account.");
            }

            if (!user.IsActive)
            {
                return AppResponses.Success("Employee system access is already inactive.", true);
            }

            var reason = string.IsNullOrWhiteSpace(command.Request.Reason)
                ? "Revoked by administrator"
                : command.Request.Reason.Trim();

            user.RevokeAccess(command.RevokedBy, reason);
            var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!updateResult.Succeeded)
            {
                return AppResponses.Failure<bool>(updateResult.Errors.First().Description);
            }

            await sessionService.RevokeAllUserSessionsAsync(user.Id, null, cancellationToken).ConfigureAwait(false);
            await iamUnitOfWork.TokenRepository
                .RevokeAllUserTokensAsync(user.Id, "Employee system access revoked")
                .ConfigureAwait(false);
            await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return AppResponses.Success("Employee system access revoked. Active sessions and refresh tokens have been terminated.", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogDeactivateUserError(logger, command.EmployeeId.ToString(), ex);
            throw;
        }
    }
}
