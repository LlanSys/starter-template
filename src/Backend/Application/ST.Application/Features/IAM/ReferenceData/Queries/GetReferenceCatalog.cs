using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.ReferenceData.Queries;



internal sealed class GetReferenceCatalogQueryHandler(IIamUnitOfWork unitOfWork, ILogger<GetReferenceCatalogQueryHandler> logger)
    : IRequestHandler<GetReferenceCatalogQuery, AppResponse<IReadOnlyList<ReferenceCatalogItemResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<ReferenceCatalogItemResponse>>> Handle(GetReferenceCatalogQuery query, CancellationToken cancellationToken)
    {
        try
        {
            if (!ReferenceCatalogTypes.All.Contains(query.CatalogType))
            {
                return AppResponses.Failure<IReadOnlyList<ReferenceCatalogItemResponse>>($"Catalog '{query.CatalogType}' is not supported.");
            }

            IReadOnlyList<ReferenceCatalogItemResponse> items = query.CatalogType.ToLowerInvariant() switch
            {
                ReferenceCatalogTypes.PermissionContexts => await unitOfWork.PermissionContextRepository
                    .ListAsync(
                        items => items
                            .OrderBy(static item => item.Label)
                            .Select(item => new ReferenceCatalogItemResponse(item.Id, ReferenceCatalogTypes.PermissionContexts, item.Key, item.Label, item.Description, null, null, item.IsActive)),
                        cancellationToken)
                    .ConfigureAwait(false),

                ReferenceCatalogTypes.PermissionResources => await unitOfWork.PermissionResourceRepository
                    .ListAsync(
                        items => items
                            .OrderBy(static item => item.ContextKey)
                            .ThenBy(static item => item.Label)
                            .Select(item => new ReferenceCatalogItemResponse(item.Id, ReferenceCatalogTypes.PermissionResources, item.Key, item.Label, item.Description, item.ContextKey, null, item.IsActive)),
                        cancellationToken)
                    .ConfigureAwait(false),

                ReferenceCatalogTypes.PermissionActions => await unitOfWork.PermissionActionRepository
                    .ListAsync(
                        items => items
                            .OrderBy(static item => item.Label)
                            .Select(item => new ReferenceCatalogItemResponse(item.Id, ReferenceCatalogTypes.PermissionActions, item.Key, item.Label, item.Description, null, null, item.IsActive)),
                        cancellationToken)
                    .ConfigureAwait(false),

                ReferenceCatalogTypes.MenuPlacements => await unitOfWork.MenuPlacementRepository
                    .ListAsync(
                        items => items
                            .OrderBy(static item => item.Label)
                            .Select(item => new ReferenceCatalogItemResponse(item.Id, ReferenceCatalogTypes.MenuPlacements, item.Key, item.Label, item.Description, null, null, item.IsActive)),
                        cancellationToken)
                    .ConfigureAwait(false),

                ReferenceCatalogTypes.MenuIcons => await unitOfWork.MenuIconRepository
                    .ListAsync(
                        items => items
                            .OrderBy(static item => item.Label)
                            .Select(item => new ReferenceCatalogItemResponse(item.Id, ReferenceCatalogTypes.MenuIcons, item.Key, item.Label, item.Description, null, null, item.IsActive)),
                        cancellationToken)
                    .ConfigureAwait(false),

                ReferenceCatalogTypes.MenuRoutes => await unitOfWork.MenuRouteRepository
                    .ListAsync(
                        items => items
                            .OrderBy(static item => item.PlacementKey)
                            .ThenBy(static item => item.Label)
                            .Select(item => new ReferenceCatalogItemResponse(item.Id, ReferenceCatalogTypes.MenuRoutes, item.Key, item.Label, item.Description, item.PlacementKey, item.Url, item.IsActive)),
                        cancellationToken)
                    .ConfigureAwait(false),

                _ => []
            };

            return AppResponses.Success("Reference catalog loaded.", items);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(GetReferenceCatalogQueryHandler), ex);
            throw;
        }
    }
}
