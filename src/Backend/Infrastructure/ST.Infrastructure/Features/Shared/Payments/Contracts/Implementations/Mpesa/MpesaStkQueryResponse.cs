using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Mpesa;

internal sealed record MpesaStkQueryResponse(
    [property: JsonPropertyName("ResultDesc")] string? ResultDescription,
    [property: JsonPropertyName("ResponseDescription")] string? ResponseDescription)
{
    public string NormalizedStatus => ResultDescription ?? ResponseDescription ?? "Unknown";
}
