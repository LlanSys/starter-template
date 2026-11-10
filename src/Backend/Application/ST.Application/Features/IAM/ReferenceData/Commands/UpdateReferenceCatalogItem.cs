using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.ReferenceData.Mappings;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.ReferenceData.Commands;



internal sealed class UpdateReferenceCatalogItemCommandHandler(IIamUnitOfWork unitOfWork, ILogger<UpdateReferenceCatalogItemCommandHandler> logger, TimeProvider timeProvider)
    : IRequestHandler<UpdateReferenceCatalogItemCommand, AppResponse<ReferenceCatalogItemResponse>>
{
    public async Task<AppResponse<ReferenceCatalogItemResponse>> Handle(UpdateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (!ReferenceCatalogTypes.All.Contains(command.CatalogType))
            {
                return AppResponses.Failure<ReferenceCatalogItemResponse>($"Catalog '{command.CatalogType}' is not supported.");
            }

            var parentError = await ValidateParentChangeAsync(command, cancellationToken).ConfigureAwait(false);
            if (parentError is not null)
            {
                return AppResponses.Failure<ReferenceCatalogItemResponse>(parentError);
            }

            ReferenceCatalogItemResponse? response = command.CatalogType.ToLowerInvariant() switch
            {
                ReferenceCatalogTypes.PermissionContexts => await UpdatePermissionContextAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.PermissionResources => await UpdatePermissionResourceAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.PermissionActions => await UpdatePermissionActionAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.MenuPlacements => await UpdateMenuPlacementAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.MenuIcons => await UpdateMenuIconAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.MenuRoutes => await UpdateMenuRouteAsync(command, cancellationToken).ConfigureAwait(false),
                _ => null
            };

            if (response is null)
            {
                return AppResponses.Failure<ReferenceCatalogItemResponse>("Reference catalog item was not found.");
            }

            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;
            return saved
                ? AppResponses.Success("Reference catalog item updated.", response)
                : AppResponses.Failure<ReferenceCatalogItemResponse>("Reference catalog item update failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(UpdateReferenceCatalogItemCommandHandler), ex);
            throw;
        }
    }

    private async Task<ReferenceCatalogItemResponse?> UpdatePermissionContextAsync(UpdateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.PermissionContextRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        item.Update(timeProvider.GetUtcNow(), command.Request.Label, command.Request.Description, command.Request.IsActive, command.UserId);
        await unitOfWork.PermissionContextRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse?> UpdatePermissionResourceAsync(UpdateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.PermissionResourceRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        item.Update(timeProvider.GetUtcNow(), command.Request.Label, command.Request.ParentKey ?? item.ContextKey, command.Request.Description, command.Request.IsActive, command.UserId);
        await unitOfWork.PermissionResourceRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse?> UpdatePermissionActionAsync(UpdateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.PermissionActionRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        item.Update(timeProvider.GetUtcNow(), command.Request.Label, command.Request.Description, command.Request.IsActive, command.UserId);
        await unitOfWork.PermissionActionRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse?> UpdateMenuPlacementAsync(UpdateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.MenuPlacementRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        item.Update(timeProvider.GetUtcNow(), command.Request.Label, command.Request.Description, command.Request.IsActive, command.UserId);
        await unitOfWork.MenuPlacementRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse?> UpdateMenuIconAsync(UpdateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.MenuIconRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        item.Update(timeProvider.GetUtcNow(), command.Request.Label, command.Request.Description, command.Request.IsActive, command.UserId);
        await unitOfWork.MenuIconRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse?> UpdateMenuRouteAsync(UpdateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.MenuRouteRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        item.Update(timeProvider.GetUtcNow(), command.Request.Label, command.Request.Url ?? item.Url, command.Request.ParentKey ?? item.PlacementKey, command.Request.Description, command.Request.IsActive, command.UserId);
        await unitOfWork.MenuRouteRepository.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<string?> ValidateParentChangeAsync(UpdateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        if (command.CatalogType.Equals(ReferenceCatalogTypes.PermissionResources, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(command.Request.ParentKey))
            {
                return "Permission resource requires a permission context.";
            }

            var item = await unitOfWork.PermissionResourceRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (item is not null && !string.Equals(item.ContextKey, command.Request.ParentKey.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var used = await unitOfWork.PermissionRepository
                    .AnyAsync(permission => permission.Context == item.ContextKey && permission.Resource == item.Key, cancellationToken)
                    .ConfigureAwait(false);

                if (used)
                {
                    return "This resource is already used by permissions, so its context cannot be changed.";
                }
            }

            var contextExists = await unitOfWork.PermissionContextRepository
                .AnyAsync(context => context.IsActive && context.Key == command.Request.ParentKey.Trim(), cancellationToken)
                .ConfigureAwait(false);

            return contextExists ? null : $"Permission context '{command.Request.ParentKey}' is not registered.";
        }

        if (command.CatalogType.Equals(ReferenceCatalogTypes.MenuRoutes, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(command.Request.ParentKey) || string.IsNullOrWhiteSpace(command.Request.Url))
            {
                return "Menu route requires a placement and URL.";
            }

            var item = await unitOfWork.MenuRouteRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (item is not null
                && (!string.Equals(item.PlacementKey, command.Request.ParentKey.Trim(), StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(item.Url, command.Request.Url.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                var used = await unitOfWork.MenuRepository
                    .AnyAsync(menu => menu.Placement == item.PlacementKey && menu.Url == item.Url, cancellationToken)
                    .ConfigureAwait(false);

                if (used)
                {
                    return "This route is already used by menus, so its placement or URL cannot be changed.";
                }
            }

            var placementExists = await unitOfWork.MenuPlacementRepository
                .AnyAsync(placement => placement.IsActive && placement.Key == command.Request.ParentKey.Trim(), cancellationToken)
                .ConfigureAwait(false);

            return placementExists ? null : $"Menu placement '{command.Request.ParentKey}' is not registered.";
        }

        return null;
    }
}
