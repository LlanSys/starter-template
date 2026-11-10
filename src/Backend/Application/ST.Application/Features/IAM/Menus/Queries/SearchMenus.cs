using System.Collections.ObjectModel;
using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
namespace ST.Application.Features.IAM.Menus.Queries;
internal sealed class SearchMenusQueryHandler(IEffectiveNavigation navigation) : IRequestHandler<SearchMenusQuery, AppResponse<PagedResponse<MenuResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<MenuResponse, Guid>>> Handle(SearchMenusQuery query, CancellationToken cancellationToken)
    {
        var request = query.SearchRequest; var size = Math.Clamp(request.PageSize, 1, 100);
        var rows = (await navigation.GetAsync(cancellationToken).ConfigureAwait(false)).Where(x => x.Available).Select(x => x.ToResponse())
            .Where(x => string.IsNullOrWhiteSpace(request.GlobalSearch) || x.Title.Contains(request.GlobalSearch, StringComparison.OrdinalIgnoreCase) || x.Key.Contains(request.GlobalSearch, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(request.Placement) || x.Placement == request.Placement)
            .Where(x => !request.ParentId.HasValue || x.ParentId == request.ParentId)
            .Where(x => !request.IsActive.HasValue || x.IsActive == request.IsActive).OrderBy(x => x.Id).ToArray();
        var page = rows.Where(x => !request.Cursor.HasValue || x.Id.CompareTo(request.Cursor.Value) > 0).Take(size + 1).ToList();
        var hasNext = page.Count > size; if (hasNext) page.RemoveAt(size);
        var result = new PagedResponse<MenuResponse, Guid> { Items = new Collection<MenuResponse>(page), TotalRecords = rows.Length, PageSize = size, CurrentPage = 1, TotalPages = (rows.Length + size - 1) / size, HasNextPage = hasNext, IsFirstPage = request.Cursor is null, NextCursor = hasNext ? page[^1].Id : Guid.Empty };
        return AppResponses.Success(result);
    }
}
