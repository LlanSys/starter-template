using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Mpesa;

internal sealed record MpesaC2BSimulateRequest(
    [property: JsonPropertyName("ShortCode")] string ShortCode,
    [property: JsonPropertyName("CommandID")] string CommandId,
    [property: JsonPropertyName("Amount")] string Amount,
    [property: JsonPropertyName("Msisdn")] string Msisdn,
    [property: JsonPropertyName("BillRefNumber")] string BillRefNumber);
