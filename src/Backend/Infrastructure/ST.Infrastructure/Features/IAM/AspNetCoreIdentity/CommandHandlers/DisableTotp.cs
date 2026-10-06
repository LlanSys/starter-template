using ST.Application.Features.IAM.Users.Commands;
using ST.Infrastructure.Configuration;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class DisableTotp(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IIamUnitOfWork iamUnitOfWork,
    IOptions<MfaSettings> mfaSettings,
    ILogger<DisableTotp> logger) : IRequestHandler<DisableTotpCommand, AppResponse<bool>>
{
    private readonly MfaSettings _mfaSettings = mfaSettings.Value;

    public async Task<AppResponse<bool>> Handle(DisableTotpCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null)
            {
                return AppResponses.Failure<bool>("User not found.");
            }

            var isEnabled = await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
            if (!isEnabled)
            {
                return AppResponses.Success("Authenticator app is already disabled.", true);
            }

            var identityResult = await userManager.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join("; ", identityResult.Errors.Select(static error => error.Description));
                return AppResponses.Failure<bool>($"Could not disable authenticator app: {errors}");
            }

            var secretsDeactivated = await iamUnitOfWork.AppUserTotpSecretRepository.DeactivateUserSecretsAsync(user.Id).ConfigureAwait(false);
            if (!secretsDeactivated)
            {
                await userManager.SetTwoFactorEnabledAsync(user, true).ConfigureAwait(false);
                return AppResponses.Failure<bool>("Could not disable authenticator app. Please try again.");
            }

            await iamUnitOfWork.TempTotpSecretRepository.DeleteUserTempSecretsAsync(user.Id, cancellationToken).ConfigureAwait(false);
            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var requiresMfa = RequiresMfa(roles);

            if (requiresMfa)
            {
                await userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
                await iamUnitOfWork.TokenRepository
                    .RevokeAllUserTokensAsync(user.Id, "MFA disabled for MFA-required account")
                    .ConfigureAwait(false);

                var activeSessions = await iamUnitOfWork.SessionRepository
                    .GetActiveSessionsByUserIdAsync(user.Id, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var session in activeSessions)
                {
                    session.Revoke("MFA disabled for MFA-required account.");
                }

                await iamUnitOfWork.SessionRepository
                    .UpdateRangeAsync(new Collection<AppUserSession>(activeSessions), cancellationToken)
                    .ConfigureAwait(false);
            }

            await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            await signInManager.ForgetTwoFactorClientAsync().ConfigureAwait(false);

            SecurityLogDefinitions.LogSecurityEvent(
                logger,
                "TotpDisabled",
                user.Id,
                $"Authenticator app disabled by {command.DisabledBy}");

            var message = requiresMfa
                ? "Authenticator app has been disabled. Because your role requires MFA, please sign in again and set it up before continuing."
                : "Authenticator app has been disabled.";

            return AppResponses.Success(message, true);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogTotpDisableError(logger, command.UserId, ex);
            throw;
        }
    }

    private bool RequiresMfa(IEnumerable<string> roles)
    {
        return _mfaSettings.Enabled
            && _mfaSettings.EnforceEnrollment
            && roles.Any(role => _mfaSettings.RequiredRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
