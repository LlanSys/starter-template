using System.Collections.ObjectModel;
using MediatR;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.IAM.Permissions.Mappings;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
namespace ST.Application.Features.IAM.Permissions.Queries;
internal sealed class SearchPermissionsQueryHandler(IIamUnitOfWork unit, IEffectiveModulePolicy policy) : IRequestHandler<SearchPermissionsQuery, AppResponse<PagedResponse<PermissionResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<PermissionResponse, Guid>>> Handle(SearchPermissionsQuery query, CancellationToken cancellationToken)
    {
        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        var owned = await unit.PermissionRepository.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var shared = access.Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Permission).Select(x => x.ToPermissionResponse());
        var request = query.SearchRequest; var size = Math.Clamp(request.PageSize, 1, 100);
        var rows = shared.Concat(owned.Select(x => x.ToPermissionResponse())).DistinctBy(x => x.Key)
            .Where(x => access.PermissionAvailable(x.Key))
            .Where(x => string.IsNullOrWhiteSpace(request.Context) || x.Context.Equals(request.Context, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(request.Resource) || x.Resource.Equals(request.Resource, StringComparison.OrdinalIgnoreCase))
            .Where(x => request.IsActive is null || x.IsActive == request.IsActive)
            .Where(x => string.IsNullOrWhiteSpace(request.GlobalSearch) || x.Key.Contains(request.GlobalSearch, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(request.GlobalSearch, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Id).ToArray();
        var page = rows.Where(x => request.Cursor is null || x.Id.CompareTo(request.Cursor.Value) > 0).Take(size + 1).ToList();
        var next = page.Count > size; if (next) page.RemoveAt(size);
        return AppResponses.Success(new PagedResponse<PermissionResponse, Guid> { Items = new Collection<PermissionResponse>(page), TotalRecords = rows.Length, PageSize = size, CurrentPage = 1, TotalPages = (rows.Length + size - 1) / size, HasNextPage = next, IsFirstPage = request.Cursor is null, NextCursor = next ? page[^1].Id : Guid.Empty });
    }
}
