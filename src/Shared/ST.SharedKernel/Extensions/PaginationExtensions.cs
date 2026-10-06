using ST.SharedKernel.Dtos.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ST.SharedKernel.Extensions;

public static class PaginationExtensions
{
    /// <summary>
    /// Trims the fetched collection (if it exceeded pageSize), maps the entities to DTOs, and wraps them in a PagedResponse.
    /// Assumes the query fetched (pageSize + 1) items to check for next page existence.
    /// </summary>
    /// <typeparam name="TEntity">The database entity type.</typeparam>
    /// <typeparam name="TResponse">The mapped DTO response type.</typeparam>
    /// <typeparam name="TCursor">The type of the cursor.</typeparam>
    /// <param name="items">The items returned from the database (should be up to pageSize + 1).</param>
    /// <param name="selector">Function to map entity to response DTO.</param>
    /// <param name="cursorSelector">Function to extract the cursor value from the last response DTO.</param>
    /// <param name="totalCount">The total records in the database.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <param name="currentCursor">The cursor used in the request (null if first page).</param>
    /// <returns>A PagedResponse ready to be returned to the client.</returns>
    public static PagedResponse<TResponse, TCursor> ToPagedResponse<TEntity, TResponse, TCursor>(
        this IList<TEntity> items,
        Func<TEntity, TResponse> selector,
        Func<TResponse, TCursor> cursorSelector,
        int totalCount,
        int pageSize,
        TCursor? currentCursor) where TCursor : struct
    {
        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
        {
            items.RemoveAt(items.Count - 1);
        }

        var projectedItems = items.Select(selector).ToArray();

        var nextCursor = hasNextPage && projectedItems.Length > 0
            ? (TCursor?)cursorSelector(projectedItems[^1])
            : null;

        var isFirstPage = currentCursor == null || currentCursor.Value.Equals(default(TCursor));

        return new PagedResponse<TResponse, TCursor>(
            new Collection<TResponse>(projectedItems),
            totalCount,
            1,
            pageSize,
            isFirstPage,
            nextCursor ?? default(TCursor));
    }

    /// <summary>
    /// String-cursor specific implementation.
    /// </summary>
    public static PagedResponse<TResponse, string> ToPagedResponse<TEntity, TResponse>(
        this IList<TEntity> items,
        Func<TEntity, TResponse> selector,
        Func<TResponse, string> cursorSelector,
        int totalCount,
        int pageSize,
        string? currentCursor)
    {
        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
        {
            items.RemoveAt(items.Count - 1);
        }

        var projectedItems = items.Select(selector).ToArray();

        var nextCursor = hasNextPage && projectedItems.Length > 0
            ? cursorSelector(projectedItems[^1])
            : null;

        var isFirstPage = string.IsNullOrEmpty(currentCursor);

        return new PagedResponse<TResponse, string>(
            new Collection<TResponse>(projectedItems),
            totalCount,
            1,
            pageSize,
            isFirstPage,
            nextCursor ?? string.Empty);
    }
}
