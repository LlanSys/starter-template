using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.IAM.Users.Commands;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ST.Api.Features.IAM.Users.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iam/users/totp")]
[ApiController]
public sealed class TotpController(ISender sender) : BaseController
{
    [HttpPost("setup")]
    [Authorize]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> InitiateSetup()
    {
        var response = await sender
            .Send(new InitiateTotpSetupCommand(GetUserId()))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("{userId}/setup")]
    [Authorize]
    [RequirePermission("users.edit")]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> InitiateSetupForUser(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var response = await sender
            .Send(new InitiateTotpSetupCommand(userId))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> Verify(VerifyOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new VerifyOtpCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("disable")]
    [Authorize]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> Disable()
    {
        var userId = GetUserId();
        var response = await sender
            .Send(new DisableTotpCommand(userId, userId))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("{userId}/status")]
    [Authorize]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> GetStatus(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var currentUserId = GetUserId();
        if (!string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase)
            && !User.IsInRole("System Administrator")
            && !User.HasClaim(static claim =>
                string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase)
                && string.Equals(claim.Value, "users.view", StringComparison.OrdinalIgnoreCase)))
        {
            return Forbid();
        }

        var response = await sender
            .Send(new GetOtpStatusCommand(userId))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id was not found.");
        }

        return userId;
    }
}
