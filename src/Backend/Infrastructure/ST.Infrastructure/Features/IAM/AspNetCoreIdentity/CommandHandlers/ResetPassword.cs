using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Events;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ResetPassword(UserManager<AppUser> users, IIamUnitOfWork unitOfWork,
    ICacheService cache, IPublisher publisher, ICurrentTenantProvider tenantProvider, ILogger<ResetPassword> logger, TimeProvider timeProvider)
    : IRequestHandler<ResetPasswordCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        try
        {
            var user = await users.FindByEmailAsync(request.Email).ConfigureAwait(false);
            if (user is null || !user.IsActive || user.IsDeleted || user.TenantId == Guid.Empty
                || user.TenantId != tenantProvider.TenantId || string.IsNullOrWhiteSpace(request.Token))
                return Rejected();
            var password = request.Password ?? request.NewPassword ?? string.Empty;
            if (!string.Equals(password, request.ConfirmPassword, StringComparison.Ordinal)) return Rejected();
            if (await users.CheckPasswordAsync(user, password).ConfigureAwait(false))
                return AppResponses.Failure<bool>("New password must be different from your current password.");

            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var now = timeProvider.GetUtcNow();
                user.CompletePasswordReset(now, user.Id);
                var result = await users.ResetPasswordAsync(user, request.Token, password).ConfigureAwait(false);
                if (!result.Succeeded) throw new ResetRejectedException();
                await AccountAccessCleanup.StageAsync(unitOfWork, user.Id, user.Id, "Password reset", now, cancellationToken).ConfigureAwait(false);
                await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                await publisher.Publish(new PasswordResetSuccessEvent(user.Id, user.Email!,
                    $"{user.FirstName} {user.LastName}", now), cancellationToken).ConfigureAwait(false);
                await cache.RemoveAsync(CacheKeys.PasswordResetOtp(user.Id), cancellationToken).ConfigureAwait(false);
                await cache.RemoveAsync(CacheKeys.PasswordResetRateLimit(user.Id), cancellationToken).ConfigureAwait(false);
                return AppResponses.Success("Password reset successfully.", true);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (ResetRejectedException ex)
        {
            ServiceLogDefinitions.LogUnexpectedTokenValidationError(logger, ex);
            return Rejected();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ServiceLogDefinitions.LogUnexpectedTokenValidationError(logger, ex);
            return AppResponses.Failure<bool>("Password reset failed. Please try again.");
        }
    }
    private static AppResponse<bool> Rejected() => AppResponses.Failure<bool>("The password reset request is invalid or expired, or the password does not meet the account policy.");
    private sealed class ResetRejectedException : Exception { }
}
