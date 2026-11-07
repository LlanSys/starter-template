using Asp.Versioning;
using ST.Api.Common.Controllers;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Queries;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ST.Api.Features.IAM.Users.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iam/auth")]
[ApiController]
public sealed class AuthController(ISender sender) : BaseController
{
    [HttpPost("invitation/accept")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> AcceptInvitation(AcceptAccountInvitationRequest request, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new AcceptAccountInvitationCommand(request), cancellationToken).ConfigureAwait(false));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login(LoginApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new LoginCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [EnableRateLimiting("RefreshTokenPolicy")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new RefreshTokenCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var response = await sender
            .Send(new LogoutCommand())
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var response = await sender
            .Send(new GetCurrentUserCommand())
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("email-otp/send")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> SendEmailOtp(SendEmailOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new SendEmailOtpCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("email-otp/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> VerifyEmailOtp(VerifyEmailOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new VerifyEmailOtpCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("otp-status/{userId}")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> GetOtpStatus(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var response = await sender
            .Send(new GetOtpStatusCommand(userId))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/verify")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> VerifyPassword(VerifyPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new VerifyPasswordCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/change")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new ChangePasswordCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new ResetPasswordCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new ForgotPasswordCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/reset/verify-otp")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    public async Task<IActionResult> VerifyPasswordResetOtp(VerifyPasswordResetOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new VerifyPasswordResetOtpCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("sso/exchange")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> ExchangeSsoCode([FromBody] ExchangeSsoCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new ExchangeSsoCodeCommand(request.Code), cancellationToken)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("passkey/register/options")]
    [Authorize]
    public async Task<IActionResult> RequestPasskeyRegistrationOptions()
    {
        var response = await sender
            .Send(new RequestPasskeyRegistrationOptionsCommand())
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("passkey/register")]
    [Authorize]
    public async Task<IActionResult> RegisterPasskey([FromBody] RegisterPasskeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new RegisterPasskeyCommand(request.AttestationResponse))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("passkey/login/options")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> RequestPasskeyLoginOptions([FromBody] RequestPasskeyLoginOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new RequestPasskeyLoginOptionsCommand(request.Username))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("passkey/login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> LoginWithPasskey([FromBody] LoginWithPasskeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new LoginWithPasskeyCommand(request.Username, request.CorrelationId, request.AssertionResponse))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("passkeys")]
    [Authorize]
    public async Task<IActionResult> GetPasskeys()
    {
        var response = await sender
            .Send(new GetPasskeysQuery())
            .ConfigureAwait(false);

        return HandleResponse(response);
    }
}
