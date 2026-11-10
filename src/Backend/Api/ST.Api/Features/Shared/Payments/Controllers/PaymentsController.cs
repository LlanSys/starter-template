using Asp.Versioning;

using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Api.Logging;
using ST.Api.Middleware;
using ST.Application.Features.Shared.Payments.CommandHandlers;
using ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;
using ST.Application.Features.Shared.Payments.QueryHandlers;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using System.Text;
using System.Text.Json;

namespace ST.Api.Features.Shared.Payments.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shared/payments")]
[ApiController]
[RequireModule("payments", "/payments")]
[Authorize]
public sealed class PaymentsController(ISender sender, ILogger<PaymentsController> logger) : BaseController
{
    [HttpPost("checkout")]
    [RequirePermission("payments.create")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> Checkout(PaymentInitiationRequest request, CancellationToken ct)
    {
        var response = await sender
            .Send(new InitiatePaymentCommand(request), ct)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("{provider}/{paymentReference}")]
    [RequirePermission("payments.view")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> Status(string provider, string paymentReference, CancellationToken ct)
    {
        var response = await sender
            .Send(new GetPaymentStatusQuery(provider, paymentReference), ct)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("capabilities")]
    [RequirePermission("payments.view")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> Capabilities(CancellationToken ct)
    {
        var response = await sender
            .Send(new GetPaymentCapabilitiesQuery(), ct)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("history")]
    [RequirePermission("payments.view")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> History([FromQuery] GetPaymentHistoryQuery query, CancellationToken ct = default)
    {
        var response = await sender
            .Send(query, ct)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("stripe/webhook")]
    [AllowAnonymous]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> StripeWebhook(CancellationToken ct)
    {
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        Request.EnableBuffering();
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var payload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        Request.Body.Position = 0;

        var response = await sender
            .Send(new ProcessPaymentWebhookCommand("Stripe", payload, signatureHeader), ct)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("mobile-money/stk-callback")]
    [AllowAnonymous]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> MpesaStkCallback([FromBody] JsonElement payload, CancellationToken ct)
    {
        var response = await sender
            .Send(new ProcessMpesaStkCallbackCommand(payload), ct)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("mobile-money/c2b-validation")]
    [AllowAnonymous]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> MpesaC2BValidation([FromBody] JsonElement payload, CancellationToken ct)
    {
        var response = await sender
            .Send(new ProcessMpesaC2BValidationCommand(payload), ct)
            .ConfigureAwait(false);

        return HandleResponse(
            response,
            onSuccess: _ => Ok(new { ResultCode = "0", ResultDesc = "Accepted" }),
            onError: _ => Ok(new { ResultCode = "C2B00016", ResultDesc = "Rejected" })
        );
    }

    [HttpPost("mobile-money/c2b-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> MpesaC2BConfirmation([FromBody] JsonElement payload, CancellationToken ct)
    {
        var response = await sender
            .Send(new ProcessMpesaC2BConfirmationCommand(payload), ct)
            .ConfigureAwait(false);

        return HandleResponse(
            response,
            onSuccess: _ => Ok(new { ResultCode = "0", ResultDesc = "Accepted" }),
            onError: error =>
            {
                PaymentLogDefinitions.LogMpesaC2bConfirmationError(logger, error.Code);
                return Ok(new { ResultCode = "0", ResultDesc = "Accepted" });
            });
    }

    [HttpPost("mobile-money/admin/register-c2b-urls")]
    [RequirePermission("payments.admin")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> MpesaRegisterC2BUrls(CancellationToken ct)
    {
        var response = await sender
            .Send(new RegisterMpesaC2BUrlsCommand(), ct)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("mobile-money/admin/simulate-c2b")]
    [RequirePermission("payments.admin")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> MpesaSimulateC2B([FromBody] SimulateMpesaC2BPaymentCommand command, CancellationToken ct)
    {
        var response = await sender
            .Send(command, ct)
            .ConfigureAwait(false);

        return HandleResponse(response);
    }
}
