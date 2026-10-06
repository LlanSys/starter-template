using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Events;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ResetPassword(
    UserManager<AppUser> userManager,
    IIamUnitOfWork iamUnitOfWork,
    ICacheService cacheService,
    IHttpContextAccessor httpContextAccessor,
    IPublisher publisher,
    ILogger<ResetPassword> logger) : IRequestHandler<ResetPasswordCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var request = command.Request;

        try
        {
            var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
            if (user == null)
            {
                ServiceLogDefinitions.LogInvalidToken(logger);
                return AppResponses.Failure<bool>("Invalid reset request");
            }

            var password = request.Password ?? request.NewPassword ?? string.Empty;

            var isSamePassword = await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false);
            if (isSamePassword)
            {
                return AppResponses.Failure<bool>("New password must be different from your current password");
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                ServiceLogDefinitions.LogInvalidToken(logger);
                return AppResponses.Failure<bool>("The password reset request is invalid or expired.");
            }

            var passwordResult = await userManager.ResetPasswordAsync(user, request.Token, password).ConfigureAwait(false);

            if (!passwordResult.Succeeded)
            {
                var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                ServiceLogDefinitions.LogFailedToAddClaim(logger, "password", "reset", user.Id, errors);
                return AppResponses.Failure<bool>("Password reset failed. Please ensure your password meets all requirements.");
            }

            user.CompletePasswordReset(user.Id);

            await userManager.UpdateAsync(user).ConfigureAwait(false);

            await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
            {
                var refreshTokens = await iamUnitOfWork.TokenRepository.GetActiveTokensByUserIdAsync(user.Id).ConfigureAwait(false);
                if (refreshTokens.Any())
                {
                    var revokedByIp = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                    await iamUnitOfWork.TokenRepository.RevokeTokensAsync(refreshTokens, "Password reset", revokedByIp).ConfigureAwait(false);
                }
                return true;

            }).ConfigureAwait(false);

            await cacheService.RemoveAsync(CacheKeys.PasswordResetOtp(user.Id), ct).ConfigureAwait(false);
            await cacheService.RemoveAsync(CacheKeys.PasswordResetRateLimit(user.Id), ct).ConfigureAwait(false);

            await publisher.Publish(new PasswordResetSuccessEvent(
                user.Id,
                user.Email!,
                $"{user.FirstName} {user.LastName}"), ct).ConfigureAwait(false);

            ServiceLogDefinitions.LogEmailOtpSent(logger, user.Id, "PasswordReset");
            return AppResponses.Success("Password reset successfully", true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogUnexpectedTokenValidationError(logger, ex);
            return AppResponses.Failure<bool>("Password reset failed. Please try again.");
        }
    }
}
