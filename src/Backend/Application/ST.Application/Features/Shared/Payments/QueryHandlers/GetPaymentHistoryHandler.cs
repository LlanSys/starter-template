using ST.Domain.Features.Shared.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

using System.Linq.Expressions;
using ST.SharedKernel.Extensions;
using MediatR;

namespace ST.Application.Features.Shared.Payments.QueryHandlers;

internal sealed class GetPaymentHistoryHandler(ISharedUnitOfWork sharedUnitOfWork)
    : IRequestHandler<GetPaymentHistoryQuery, AppResponse<PagedResponse<PaymentHistoryItemResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<PaymentHistoryItemResponse, Guid>>> Handle(
        GetPaymentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var repository = sharedUnitOfWork.PaymentRecordRepository;

        Expression<Func<ST.Domain.Features.Shared.Payments.Entities.PaymentRecord, bool>> filterExpression = record =>
            (string.IsNullOrWhiteSpace(request.SearchTerm) || record.CustomerReference.Contains(request.SearchTerm) || (record.ProviderReference != null && record.ProviderReference.Contains(request.SearchTerm))) &&
            (string.IsNullOrWhiteSpace(request.Provider) || record.Provider == request.Provider) &&
            (!request.ExactAmount.HasValue || record.Amount.Amount == request.ExactAmount.Value) &&
            (!request.MinAmount.HasValue || record.Amount.Amount >= request.MinAmount.Value) &&
            (!request.MaxAmount.HasValue || record.Amount.Amount <= request.MaxAmount.Value) &&
            (!request.StartDate.HasValue || record.CreatedAt >= request.StartDate.Value) &&
            (!request.EndDate.HasValue || record.CreatedAt <= request.EndDate.Value) &&
            (!request.Status.HasValue || record.Status == request.Status.Value);

        var totalCount = await repository.CountAsync(filterExpression, cancellationToken).ConfigureAwait(false);

        var cursorRecord = request.Cursor.HasValue
            ? await repository.FindByIdAsync(request.Cursor.Value, cancellationToken).ConfigureAwait(false)
            : null;

        var records = await repository.ListAsync(
            query =>
            {
                var q = query.Where(filterExpression)
                             .OrderByDescending(record => record.CreatedAt)
                             .ThenByDescending(record => record.Id);

                if (cursorRecord != null)
                {
                    q = (System.Linq.IOrderedQueryable<ST.Domain.Features.Shared.Payments.Entities.PaymentRecord>)q.Where(record =>
                        record.CreatedAt < cursorRecord.CreatedAt ||
                        (record.CreatedAt == cursorRecord.CreatedAt && record.Id.CompareTo(cursorRecord.Id) < 0));
                }

                return q.Take(pageSize + 1);
            },
            cancellationToken).ConfigureAwait(false);

        var hasNextPage = records.Count > pageSize;
        if (hasNextPage)
        {
            records.RemoveAt(records.Count - 1);
        }

        var items = records
            .Select(record => new PaymentHistoryItemResponse(
                record.Id,
                record.CustomerReference,
                record.Provider,
                record.Amount.Amount,
                record.Amount.Currency,
                record.Description,
                record.Status.ToString(),
                record.CheckoutUrl ?? string.Empty,
                record.CreatedAt,
                record.UpdatedAt))
            .ToArray();

        var isFirstPage = !request.Cursor.HasValue;
        var nextCursor = hasNextPage ? records[^1].Id : (Guid?)null;

        var response = new PagedResponse<PaymentHistoryItemResponse, Guid>(
            new System.Collections.ObjectModel.Collection<PaymentHistoryItemResponse>(items),
            totalCount,
            1,
            pageSize,
            isFirstPage,
            nextCursor ?? Guid.Empty);

        return AppResponses.Success(response);
    }
}
