using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.Domain.Features.IAM.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.ReferenceData.Commands;



internal sealed class DeleteReferenceCatalogItemCommandHandler(IIamUnitOfWork unitOfWork, ILogger<DeleteReferenceCatalogItemCommandHandler> logger, TimeProvider timeProvider)
    : IRequestHandler<DeleteReferenceCatalogItemCommand, AppResponse<bool>>
{
    public async Task<AppResponse<bool>> Handle(DeleteReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var usageError = await ValidateNotUsedAsync(command, cancellationToken).ConfigureAwait(false);
            if (usageError is not null)
            {
                return AppResponses.Failure<bool>(usageError);
            }

            var found = await DeactivateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!found)
            {
                return AppResponses.Failure<bool>("Reference catalog item was not found.");
            }

            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;
            return saved
                ? AppResponses.Success("Reference catalog item deactivated.", true)
                : AppResponses.Failure<bool>("Reference catalog item deactivate failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(DeleteReferenceCatalogItemCommandHandler), ex);
            throw;
        }
    }

    private async Task<bool> DeactivateAsync(DeleteReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        switch (command.CatalogType.ToLowerInvariant())
        {
            case ReferenceCatalogTypes.PermissionContexts:
                var context = await unitOfWork.PermissionContextRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
                if (context is null) return false;
                context.Update(timeProvider.GetUtcNow(), context.Label, context.Description, false, command.UserId);
                await unitOfWork.PermissionContextRepository.UpdateAsync(context, cancellationToken).ConfigureAwait(false);
                return true;

            case ReferenceCatalogTypes.PermissionResources:
                var resource = await unitOfWork.PermissionResourceRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
                if (resource is null) return false;
                resource.Update(timeProvider.GetUtcNow(), resource.Label, resource.ContextKey, resource.Description, false, command.UserId);
                await unitOfWork.PermissionResourceRepository.UpdateAsync(resource, cancellationToken).ConfigureAwait(false);
                return true;

            case ReferenceCatalogTypes.PermissionActions:
                var action = await unitOfWork.PermissionActionRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
                if (action is null) return false;
                action.Update(timeProvider.GetUtcNow(), action.Label, action.Description, false, command.UserId);
                await unitOfWork.PermissionActionRepository.UpdateAsync(action, cancellationToken).ConfigureAwait(false);
                return true;

            case ReferenceCatalogTypes.MenuPlacements:
                var placement = await unitOfWork.MenuPlacementRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
                if (placement is null) return false;
                placement.Update(timeProvider.GetUtcNow(), placement.Label, placement.Description, false, command.UserId);
                await unitOfWork.MenuPlacementRepository.UpdateAsync(placement, cancellationToken).ConfigureAwait(false);
                return true;

            case ReferenceCatalogTypes.MenuIcons:
                var icon = await unitOfWork.MenuIconRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
                if (icon is null) return false;
                icon.Update(timeProvider.GetUtcNow(), icon.Label, icon.Description, false, command.UserId);
                await unitOfWork.MenuIconRepository.UpdateAsync(icon, cancellationToken).ConfigureAwait(false);
                return true;

            case ReferenceCatalogTypes.MenuRoutes:
                var route = await unitOfWork.MenuRouteRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
                if (route is null) return false;
                route.Update(timeProvider.GetUtcNow(), route.Label, route.Url, route.PlacementKey, route.Description, false, command.UserId);
                await unitOfWork.MenuRouteRepository.UpdateAsync(route, cancellationToken).ConfigureAwait(false);
                return true;

            default:
                return false;
        }
    }

    private async Task<string?> ValidateNotUsedAsync(DeleteReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        return command.CatalogType.ToLowerInvariant() switch
        {
            ReferenceCatalogTypes.PermissionContexts => await ValidatePermissionContextAsync(command.Id, cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.PermissionResources => await ValidatePermissionResourceAsync(command.Id, cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.PermissionActions => await ValidatePermissionActionAsync(command.Id, cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.MenuPlacements => await ValidateMenuPlacementAsync(command.Id, cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.MenuIcons => await ValidateMenuIconAsync(command.Id, cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.MenuRoutes => await ValidateMenuRouteAsync(command.Id, cancellationToken).ConfigureAwait(false),
            _ => $"Catalog '{command.CatalogType}' is not supported."
        };
    }

    private async Task<string?> ValidatePermissionContextAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.PermissionContextRepository.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        var used = await unitOfWork.PermissionRepository.AnyAsync(permission => permission.Context == item.Key, cancellationToken).ConfigureAwait(false)
            || await unitOfWork.PermissionResourceRepository.AnyAsync(resource => resource.ContextKey == item.Key, cancellationToken).ConfigureAwait(false);
        return used ? "This context is used by permissions or resources and cannot be deactivated." : null;
    }

    private async Task<string?> ValidatePermissionResourceAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.PermissionResourceRepository.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        var used = await unitOfWork.PermissionRepository.AnyAsync(permission => permission.Context == item.ContextKey && permission.Resource == item.Key, cancellationToken).ConfigureAwait(false);
        return used ? "This resource is used by permissions and cannot be deactivated." : null;
    }

    private async Task<string?> ValidatePermissionActionAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.PermissionActionRepository.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        var used = await unitOfWork.PermissionRepository.AnyAsync(permission => permission.Action == item.Key, cancellationToken).ConfigureAwait(false);
        return used ? "This action is used by permissions and cannot be deactivated." : null;
    }

    private async Task<string?> ValidateMenuPlacementAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.MenuPlacementRepository.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        var used = await unitOfWork.MenuRepository.AnyAsync(menu => menu.Placement == item.Key, cancellationToken).ConfigureAwait(false)
            || await unitOfWork.MenuRouteRepository.AnyAsync(route => route.PlacementKey == item.Key, cancellationToken).ConfigureAwait(false);
        return used ? "This placement is used by menus or routes and cannot be deactivated." : null;
    }

    private async Task<string?> ValidateMenuIconAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.MenuIconRepository.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        var used = await unitOfWork.MenuRepository.AnyAsync(menu => menu.Icon == item.Key, cancellationToken).ConfigureAwait(false);
        return used ? "This icon is used by menus and cannot be deactivated." : null;
    }

    private async Task<string?> ValidateMenuRouteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await unitOfWork.MenuRouteRepository.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return null;
        var used = await unitOfWork.MenuRepository.AnyAsync(menu => menu.Placement == item.PlacementKey && menu.Url == item.Url, cancellationToken).ConfigureAwait(false);
        return used ? "This route is used by menus and cannot be deactivated." : null;
    }
}
