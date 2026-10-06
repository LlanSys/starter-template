using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class InitiateTotpSetupCommandHandler(
    IEncryptionService encryptionService,
    UserManager<AppUser> userManager,
    IIamUnitOfWork iamUnitOfWork,
    ILogger<InitiateTotpSetupCommandHandler> logger) : IRequestHandler<InitiateTotpSetupCommand, AppResponse<TwoFactorSetupInfo>>
{
    private const string TotpIssuer = "StarterTemplate.API";

    public async Task<AppResponse<TwoFactorSetupInfo>> Handle(InitiateTotpSetupCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;

        try
        {
            var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null || !user.IsActive || user.IsDeleted || !user.EmailConfirmed)
            {
                return AppResponses.Failure<TwoFactorSetupInfo>("User not found.");
            }

            await iamUnitOfWork.TempTotpSecretRepository.DeleteUserTempSecretsAsync(userId, cancellationToken).ConfigureAwait(false);

            var plainSecret = GenerateSecret();
            var encryptedSecret = encryptionService.Encrypt(plainSecret);

            var tempSecret = TempTotpSecret.Create(
                userId,
                encryptedSecret,
                DateTimeOffset.UtcNow.AddMinutes(30),
                userId);

            await iamUnitOfWork.TempTotpSecretRepository.CreateAsync(tempSecret, cancellationToken).ConfigureAwait(false);
            await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            var qrUri = GenerateQrCodeUri(user.Email!, plainSecret);

            return AppResponses.Success("Scan this QR code", new TwoFactorSetupInfo
            {
                QrCodeUri = qrUri,
                ManualEntryKey = plainSecret
            });
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogTotpSetupInitiationError(logger, userId, ex);
            throw;
        }
    }

    private static string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    private static string GenerateQrCodeUri(string email, string secret)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(TotpIssuer)}:" +
            $"{Uri.EscapeDataString(email)}?" +
            $"secret={secret}&" +
            $"issuer={Uri.EscapeDataString(TotpIssuer)}&" +
            "algorithm=SHA1&" +
            "digits=6&" +
            "period=30";
    }
}
