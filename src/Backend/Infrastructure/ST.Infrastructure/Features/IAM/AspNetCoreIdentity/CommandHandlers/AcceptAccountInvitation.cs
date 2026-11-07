using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class AcceptAccountInvitation(UserManager<AppUser> users, IIamUnitOfWork unitOfWork,
    ICurrentTenantProvider tenantProvider, ILogger<AcceptAccountInvitation> logger, IControlPlaneUnitOfWork controlPlane, TimeProvider timeProvider)
    : IRequestHandler<AcceptAccountInvitationCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(AcceptAccountInvitationCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        try
        {
            var user = await users.FindByIdAsync(request.UserId).ConfigureAwait(false);
            if (user is null || !user.IsActive || user.IsDeleted || user.EmailConfirmed || user.TenantId == Guid.Empty
                || user.TenantId != tenantProvider.TenantId || !string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                return Rejected();
            if (await controlPlane.Tenants.AnyAsync(
                tenant => tenant.Id == user.TenantId && tenant.AdministratorUserId == user.Id, cancellationToken).ConfigureAwait(false))
            {
                return Rejected();
            }

            if (!await users.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, AccountInvitation.Purpose(user), request.Token).ConfigureAwait(false))
                return Rejected();

            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Only possession of the protected invitation confirms this email. Never issue a login session here.
                user.EmailConfirmed = true;
                var now = timeProvider.GetUtcNow();
                user.CompletePasswordReset(now, user.Id);
                var resetToken = await users.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
                var result = await users.ResetPasswordAsync(user, resetToken, request.Password).ConfigureAwait(false);
                if (!result.Succeeded) throw new InvitationRejectedException();
                await AccountAccessCleanup.StageAsync(unitOfWork, user.Id, user.Id, "Account invitation accepted", now, cancellationToken).ConfigureAwait(false);
                await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return AppResponses.Success("Account setup complete. Sign in with your new password.", true);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (InvitationRejectedException ex)
        {
            AccountInvitationLogDefinitions.LogFailed(logger, request.UserId, ex);
            return Rejected();
        }
        catch (Exception ex)
        {
            AccountInvitationLogDefinitions.LogFailed(logger, request.UserId, ex);
            throw;
        }
    }

    private static AppResponse<bool> Rejected() => AppResponses.Failure<bool>("The invitation is invalid or expired, or the password does not meet the account policy.");
    private sealed class InvitationRejectedException : Exception { }
}
