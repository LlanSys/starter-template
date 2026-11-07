using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Enums;
using ST.Domain.Features.IAM.Users.Events;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class SendEmailOtp(
    UserManager<AppUser> userManager,
    IDistributedCache cache,
    IPublisher publisher,
    ILogger<SendEmailOtp> logger,
    TimeProvider timeProvider) : IRequestHandler<SendEmailOtpCommand, AppResponse<SendEmailOtpResponse>>
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    public async Task<AppResponse<SendEmailOtpResponse>> Handle(SendEmailOtpCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var user = await userManager.FindByIdAsync(request.UserId).ConfigureAwait(false);

        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return AppResponses.Failure<SendEmailOtpResponse>("User not found");

        if (!user.IsActive || user.IsDeleted)
            return AppResponses.Failure<SendEmailOtpResponse>("Account is disabled");

        var requiresConfirmed = !string.Equals(request.Purpose, OtpPurpose.EmailConfirmation.ToString(), StringComparison.OrdinalIgnoreCase);
        if (requiresConfirmed && !await userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false))
            return AppResponses.Failure<SendEmailOtpResponse>("Email not confirmed");

        var cooldownKey = CacheKeys.EmailOtpCooldown(user.Id);
        if (await cache.GetStringAsync(cooldownKey, ct).ConfigureAwait(false) != null)
            return AppResponses.Failure<SendEmailOtpResponse>("Please wait before requesting another code");

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var otpKey = CacheKeys.EmailOtp(user.Id);
        var hashed = HashCode(user.Id, code, request.Purpose.ToString());

        await cache.SetStringAsync(otpKey, hashed, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = OtpLifetime }, ct).ConfigureAwait(false);
        await cache.SetStringAsync(cooldownKey, "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Cooldown }, ct).ConfigureAwait(false);

        var expiresAt = timeProvider.GetUtcNow().Add(OtpLifetime);

        await publisher.Publish(new EmailOtpRequestedEvent(
            user.Id,
            user.Email!,
            user.FirstName,
            code,
            request.Purpose,
            expiresAt), ct).ConfigureAwait(false);

        ServiceLogDefinitions.LogEmailOtpSent(logger, user.Id, request.Purpose);
        return AppResponses.Success(
            "Code sent",
            new SendEmailOtpResponse(
                user.Id,
                expiresAt,
                (int)Cooldown.TotalSeconds));
    }

    private static string HashCode(string userId, string code, string purpose)
    {
        var input = $"{userId}:{purpose}:{code}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }
}
