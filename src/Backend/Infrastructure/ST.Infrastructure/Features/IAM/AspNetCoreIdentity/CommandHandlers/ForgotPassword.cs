using System.Net;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Features.Shared.Notifications.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ForgotPassword(
    UserManager<AppUser> userManager,
    ISender sender,
    IEmailService emailService,
    IOptions<PasswordRecoverySettings> recoveryOptions,
    IOptions<EmailSettings> emailOptions,
    ILogger<ForgotPassword> logger)
    : IRequestHandler<ForgotPasswordCommand, AppResponse<ForgotPasswordResponse>>
{
    private const string GenericMessage = "If an active account matches that email, recovery instructions have been sent.";
    private readonly PasswordRecoverySettings _recoverySettings = recoveryOptions.Value;
    private readonly EmailSettings _emailSettings = emailOptions.Value;

    public async Task<AppResponse<ForgotPasswordResponse>> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var response = new ForgotPasswordResponse(
            _recoverySettings.Mode.ToString(),
            _recoverySettings.Mode == PasswordRecoveryMode.EmailOtp);

        var user = await userManager.FindByEmailAsync(command.Request.Email).ConfigureAwait(false);
        if (user is null || !user.IsActive || user.IsDeleted || string.IsNullOrWhiteSpace(user.Email))
        {
            return AppResponses.Success(GenericMessage, response);
        }

        if (_recoverySettings.Mode == PasswordRecoveryMode.EmailOtp)
        {
            var otpResponse = await sender.Send(new SendEmailOtpCommand(new SendEmailOtpRequest
            {
                UserId = user.Id,
                Purpose = "PasswordReset"
            }), ct).ConfigureAwait(false);

            if (!otpResponse.IsSuccess)
            {
                ServiceLogDefinitions.LogFailedToSendEmail(logger, user.Email, otpResponse.Message ?? "Password recovery OTP could not be sent.");
            }

            return AppResponses.Success(GenericMessage, response);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        var resetUri = BuildResetUri(user.Email, token);
        var displayName = WebUtility.HtmlEncode($"{user.FirstName} {user.LastName}".Trim());
        var encodedUri = WebUtility.HtmlEncode(resetUri.ToString());

        var emailResponse = await emailService.SendEmailAsync(new SendEmailRequest
        {
            To = user.Email,
            Subject = "Reset your StarterTemplate password",
            Body = $"<p>Hello {displayName},</p><p>Use the secure link below to reset your password.</p><p><a href=\"{encodedUri}\">Reset password</a></p><p>If you did not request this, you can ignore this email.</p>"
        }, ct).ConfigureAwait(false);

        if (!emailResponse.IsSuccess)
        {
            ServiceLogDefinitions.LogFailedToSendEmail(logger, user.Email, emailResponse.Message ?? "Password recovery link could not be sent.");
        }

        return AppResponses.Success(GenericMessage, response);
    }

    private Uri BuildResetUri(string email, string token)
    {
        var baseUri = new Uri(_emailSettings.ClientBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var resetUri = new Uri(baseUri, _recoverySettings.ResetPath.TrimStart('/'));
        var query = $"email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        return new UriBuilder(resetUri) { Query = query }.Uri;
    }
}
