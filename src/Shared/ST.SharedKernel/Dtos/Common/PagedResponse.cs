using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ST.SharedKernel.Dtos.Common;

public class PagedResponse<T, TCursor>
{
    public Collection<T> Items { get; init; } = [];
    public int TotalRecords { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; init; }
    public bool IsFirstPage { get; init; }
    public bool IsLastPage => !HasNextPage;

    // Cursor properties (optional, for keyset pagination)
    public string? Cursor { get; set; }
    public TCursor? NextCursor { get; set; }

    public PagedResponse() { }

    public PagedResponse(Collection<T> items, int totalRecords, int pageNumber, int pageSize, bool isFirstPage, TCursor? nextCursor)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        Items = items;
        TotalRecords = totalRecords;
        CurrentPage = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(items.Count / (double)pageSize);
        IsFirstPage = isFirstPage;
        NextCursor = nextCursor;
        HasNextPage = nextCursor is not null;

    }
}
