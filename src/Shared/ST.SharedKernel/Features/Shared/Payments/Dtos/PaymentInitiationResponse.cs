namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record PaymentInitiationResponse(
    string Provider,
    string PaymentReference,
    string CheckoutUrl,
    string Status);
