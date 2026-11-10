using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Modules.Entities;

namespace ST.Application.Features.IAM.ReferenceData.Queries;



internal sealed class GetIamReferenceDataQueryHandler(IIamUnitOfWork unitOfWork, ILogger<GetIamReferenceDataQueryHandler> logger, IEffectiveModulePolicy policy)
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

            var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
            var standardPermissions = access.Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Permission && access.EntryAvailable(x)).Select(ModuleCatalogueMapping.ToPermissionResponse).ToArray();
            contexts = standardPermissions.Select(x => new CatalogOptionResponse(x.Context, x.Context switch { "iam" => "Identity and access", "shared" => "Shared", "platform" => "Platform", _ => x.Context })).Concat(contexts).DistinctBy(x => x.Key).ToList();
            resources = standardPermissions.Select(x => new CatalogOptionResponse(x.Resource, x.Resource, "", x.Context)).Concat(resources).DistinctBy(x => (x.Key, x.ParentKey)).ToList();
            actions = standardPermissions.Select(x => new CatalogOptionResponse(x.Action, x.Action)).Concat(actions).DistinctBy(x => x.Key).ToList();
            placements = new[] { new CatalogOptionResponse("Sidebar", "Sidebar"), new CatalogOptionResponse("AdminCenter", "Admin Center"), new CatalogOptionResponse("Features", "Features") }.Concat(placements).DistinctBy(x => x.Key).ToList();
            var availablePaths = access.AvailablePages.Where(x => !string.IsNullOrEmpty(x.Path)).Select(x => x.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            routes = access.AvailablePages.Where(x => !string.IsNullOrEmpty(x.Path)).Select(x => new CatalogOptionResponse(x.Path, x.Title, "", x.Placement))
                .Concat(routes.Where(x => availablePaths.Contains(x.Key))).DistinctBy(x => (x.Key, x.ParentKey)).ToList();
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
