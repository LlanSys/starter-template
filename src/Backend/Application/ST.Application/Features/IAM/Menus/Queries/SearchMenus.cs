using System.Collections.ObjectModel;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Queries;



internal sealed class SearchMenusQueryHandler(IIamUnitOfWork unitOfWork, ILogger<SearchMenusQueryHandler> logger)
    : IRequestHandler<SearchMenusQuery, AppResponse<PagedResponse<MenuResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<MenuResponse, Guid>>> Handle(SearchMenusQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var request = query.SearchRequest;
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            IQueryable<MenuItem> ApplyFilters(IQueryable<MenuItem> menus)
            {
                if (!string.IsNullOrWhiteSpace(request.GlobalSearch))
                {
                    var search = request.GlobalSearch.Trim();
                    menus = menus.Where(menu =>
                        menu.Key.Contains(search)
                        || menu.Title.Contains(search)
                        || menu.Description.Contains(search)
                        || menu.Url.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(request.Placement))
                {
                    menus = menus.Where(menu => menu.Placement == request.Placement);
                }

                if (request.ParentId.HasValue)
                {
                    menus = menus.Where(menu => menu.ParentId == request.ParentId.Value);
                }

                if (request.IsActive.HasValue)
                {
                    menus = menus.Where(menu => menu.IsActive == request.IsActive.Value);
                }

                return menus;
            }

            var totalCount = await unitOfWork.MenuRepository.CountAsync(ApplyFilters, cancellationToken).ConfigureAwait(false);

            IQueryable<MenuItem> ApplyPaging(IQueryable<MenuItem> menus)
            {
                menus = ApplyFilters(menus);

                if (request.Cursor.HasValue && request.Cursor != Guid.Empty)
                {
                    menus = menus.Where(menu => menu.Id.CompareTo(request.Cursor.Value) > 0);
                }

                return menus
                    .OrderBy(static menu => menu.Id)
                    .Take(pageSize + 1);
            }

            var page = await unitOfWork.MenuRepository.ListAsync(ApplyPaging, cancellationToken).ConfigureAwait(false);

            var hasNextPage = page.Count > pageSize;
            if (hasNextPage)
            {
                page.RemoveAt(page.Count - 1);
            }

            var items = page
                .Select(static menu => menu.ToMenuResponse())
                .OrderBy(static menu => menu.Placement, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static menu => menu.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nextCursor = hasNextPage ? page[^1].Id : (Guid?)null;
            var result = new PagedResponse<MenuResponse, Guid>(
                new Collection<MenuResponse>(items),
                totalCount,
                1,
                pageSize,
                request.Cursor is null || request.Cursor == Guid.Empty,
                nextCursor ?? Guid.Empty);

            return AppResponses.Success(result);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(SearchMenusQueryHandler), ex);
            throw;
        }
    }
}
