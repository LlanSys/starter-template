namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record PaymentInitiationRequest(
    decimal Amount,
    string Currency,
    string Description,
    string CallbackUrl,
    string? PayerPhoneNumber = null,
    string? Provider = null,
    string? IdempotencyKey = null);
