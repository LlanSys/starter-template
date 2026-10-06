namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record PaymentStatusResponse(
    string Provider,
    string PaymentReference,
    string Status,
    decimal Amount,
    string Currency);
