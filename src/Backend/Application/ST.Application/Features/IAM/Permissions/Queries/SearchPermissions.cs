using System.Collections.ObjectModel;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Permissions.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Permissions.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Permissions.Queries;



internal sealed class SearchPermissionsQueryHandler(IIamUnitOfWork unitOfWork, ILogger<SearchPermissionsQueryHandler> logger)
    : IRequestHandler<SearchPermissionsQuery, AppResponse<PagedResponse<PermissionResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<PermissionResponse, Guid>>> Handle(SearchPermissionsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var request = query.SearchRequest;
            var pageSize = Math.Clamp(request.PageSize, 1, 50);

            IQueryable<Permission> ApplyFilters(IQueryable<Permission> permissions)
            {
                if (!string.IsNullOrWhiteSpace(request.GlobalSearch))
                {
                    var search = request.GlobalSearch.Trim();
                    permissions = permissions.Where(permission =>
                        permission.Key.Contains(search)
                        || permission.Context.Contains(search)
                        || permission.Resource.Contains(search)
                        || permission.Action.Contains(search)
                        || permission.Description.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(request.Context))
                {
                    permissions = permissions.Where(permission => permission.Context == request.Context);
                }

                if (!string.IsNullOrWhiteSpace(request.Resource))
                {
                    permissions = permissions.Where(permission => permission.Resource == request.Resource);
                }

                if (request.IsActive.HasValue)
                {
                    permissions = permissions.Where(permission => permission.IsActive == request.IsActive.Value);
                }

                return permissions;
            }

            var totalCount = await unitOfWork.PermissionRepository.CountAsync(ApplyFilters, cancellationToken).ConfigureAwait(false);

            IQueryable<Permission> ApplyPaging(IQueryable<Permission> permissions)
            {
                permissions = ApplyFilters(permissions);

                if (request.Cursor.HasValue && request.Cursor != Guid.Empty)
                {
                    permissions = permissions.Where(permission => permission.Id.CompareTo(request.Cursor.Value) > 0);
                }

                return permissions
                    .OrderBy(static permission => permission.Id)
                    .Take(pageSize + 1);
            }

            var page = await unitOfWork.PermissionRepository.ListAsync(ApplyPaging, cancellationToken).ConfigureAwait(false);

            var hasNextPage = page.Count > pageSize;
            if (hasNextPage)
            {
                page.RemoveAt(page.Count - 1);
            }

            var items = page
                .Select(static permission => permission.ToPermissionResponse())
                .OrderBy(static permission => permission.Context, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static permission => permission.Resource, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static permission => permission.Action, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nextCursor = hasNextPage ? page[^1].Id : (Guid?)null;
            var result = new PagedResponse<PermissionResponse, Guid>(
                new Collection<PermissionResponse>(items),
                totalCount,
                1,
                pageSize,
                request.Cursor is null || request.Cursor == Guid.Empty,
                nextCursor ?? Guid.Empty);

            return AppResponses.Success(result);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(SearchPermissionsQueryHandler), ex);
            throw;
        }
    }
}
