using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Mpesa;

internal sealed record MpesaStkPushRequest(
    [property: JsonPropertyName("BusinessShortCode")] string BusinessShortCode,
    [property: JsonPropertyName("Password")] string Password,
    [property: JsonPropertyName("Timestamp")] string Timestamp,
    [property: JsonPropertyName("TransactionType")] string TransactionType,
    [property: JsonPropertyName("Amount")] int Amount,
    [property: JsonPropertyName("PartyA")] string PartyA,
    [property: JsonPropertyName("PartyB")] string PartyB,
    [property: JsonPropertyName("PhoneNumber")] string PhoneNumber,
    [property: JsonPropertyName("CallBackURL")] string CallbackUrl,
    [property: JsonPropertyName("AccountReference")] string AccountReference,
    [property: JsonPropertyName("TransactionDesc")] string TransactionDescription);
