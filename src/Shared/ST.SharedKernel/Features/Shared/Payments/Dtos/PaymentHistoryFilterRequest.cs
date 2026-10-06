namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record PaymentHistoryFilterRequest(
    Guid? Cursor = null,
    int PageSize = 20,
    string? SearchTerm = null,
    string? Provider = null,
    decimal? ExactAmount = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    string? Status = null);
