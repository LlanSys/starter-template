namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record PaymentHistoryItemResponse(
    Guid Id,
    string PaymentReference,
    string Provider,
    decimal Amount,
    string Currency,
    string Description,
    string Status,
    string CheckoutUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
