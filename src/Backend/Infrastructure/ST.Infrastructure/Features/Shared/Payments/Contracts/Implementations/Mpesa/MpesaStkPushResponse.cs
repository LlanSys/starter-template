using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Mpesa;

internal sealed record MpesaStkPushResponse(
    [property: JsonPropertyName("CheckoutRequestID")] string? CheckoutRequestId,
    [property: JsonPropertyName("ResponseDescription")] string? ResponseDescription);
