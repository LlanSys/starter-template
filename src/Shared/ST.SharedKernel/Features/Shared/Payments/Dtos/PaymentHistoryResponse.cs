namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record PaymentHistoryResponse(
    IReadOnlyCollection<PaymentHistoryItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
