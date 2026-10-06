using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Mpesa;

internal sealed record MpesaStkQueryRequest(
    [property: JsonPropertyName("BusinessShortCode")] string BusinessShortCode,
    [property: JsonPropertyName("Password")] string Password,
    [property: JsonPropertyName("Timestamp")] string Timestamp,
    [property: JsonPropertyName("CheckoutRequestID")] string CheckoutRequestId);
