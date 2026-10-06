using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using ST.Domain.Features.Shared.Payments.Enums;
using MediatR;

namespace ST.Application.Features.Shared.Payments.QueryHandlers;

public sealed record GetPaymentHistoryQuery(
    Guid? Cursor = null,
    int PageSize = 20,
    string? SearchTerm = null,
    string? Provider = null,
    decimal? ExactAmount = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    PaymentStatus? Status = null)
    : IRequest<AppResponse<PagedResponse<PaymentHistoryItemResponse, Guid>>>, ICachableRequest
{
    public string CacheGroup => "payments";
    public string Discriminator => CacheKeys.Entity("history", $"{Cursor}_{PageSize}");
    public string? CacheUserId => null;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
