using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Mpesa;

internal sealed record MpesaC2BRegisterUrlRequest(
    [property: JsonPropertyName("ShortCode")] string ShortCode,
    [property: JsonPropertyName("ResponseType")] string ResponseType,
    [property: JsonPropertyName("ConfirmationURL")] string ConfirmationUrl,
    [property: JsonPropertyName("ValidationURL")] string ValidationUrl);
