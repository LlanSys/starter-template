using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.ReferenceData.Queries;



internal sealed class GetIamReferenceDataQueryHandler(IIamUnitOfWork unitOfWork, ILogger<GetIamReferenceDataQueryHandler> logger)
    : IRequestHandler<GetIamReferenceDataQuery, AppResponse<IamReferenceDataResponse>>
{
    public async Task<AppResponse<IamReferenceDataResponse>> Handle(GetIamReferenceDataQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var contexts = await unitOfWork.PermissionContextRepository
                .ListAsync(
                    items => items
                        .Where(static item => item.IsActive)
                        .OrderBy(static item => item.Label)
                        .Select(static item => new CatalogOptionResponse(item.Key, item.Label, item.Description)),
                    cancellationToken)
                .ConfigureAwait(false);

            var resources = await unitOfWork.PermissionResourceRepository
                .ListAsync(
                    items => items
                        .Where(static item => item.IsActive)
                        .OrderBy(static item => item.ContextKey)
                        .ThenBy(static item => item.Label)
                        .Select(static item => new CatalogOptionResponse(item.Key, item.Label, item.Description, item.ContextKey)),
                    cancellationToken)
                .ConfigureAwait(false);

            var actions = await unitOfWork.PermissionActionRepository
                .ListAsync(
                    items => items
                        .Where(static item => item.IsActive)
                        .OrderBy(static item => item.Label)
                        .Select(static item => new CatalogOptionResponse(item.Key, item.Label, item.Description)),
                    cancellationToken)
                .ConfigureAwait(false);

            var placements = await unitOfWork.MenuPlacementRepository
                .ListAsync(
                    items => items
                        .Where(static item => item.IsActive)
                        .OrderBy(static item => item.Label)
                        .Select(static item => new CatalogOptionResponse(item.Key, item.Label, item.Description)),
                    cancellationToken)
                .ConfigureAwait(false);

            var icons = await unitOfWork.MenuIconRepository
                .ListAsync(
                    items => items
                        .Where(static item => item.IsActive)
                        .OrderBy(static item => item.Label)
                        .Select(static item => new CatalogOptionResponse(item.Key, item.Label, item.Description)),
                    cancellationToken)
                .ConfigureAwait(false);

            var routes = await unitOfWork.MenuRouteRepository
                .ListAsync(
                    items => items
                        .Where(static item => item.IsActive)
                        .OrderBy(static item => item.PlacementKey)
                        .ThenBy(static item => item.Label)
                        .Select(static item => new CatalogOptionResponse(item.Url, item.Label, item.Description, item.PlacementKey)),
                    cancellationToken)
                .ConfigureAwait(false);

            var response = new IamReferenceDataResponse(contexts, resources, actions, placements, icons, routes);
            return AppResponses.Success("IAM reference data loaded.", response);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(GetIamReferenceDataQueryHandler), ex);
            throw;
        }
    }
}
