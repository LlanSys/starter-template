using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Commands;



internal sealed class UpdateMenuCommandHandler(IIamUnitOfWork unitOfWork, ILogger<UpdateMenuCommandHandler> logger, IEffectiveNavigation navigation)
    : IRequestHandler<UpdateMenuCommand, AppResponse<MenuResponse>>
{
    public async Task<AppResponse<MenuResponse>> Handle(UpdateMenuCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var request = command.Request;
            var registered = (await navigation.GetAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(x => x.Menu.Id == command.Id && x.Definition is not null);
            if (registered?.Definition is { } page)
                return await navigation.SaveOverrideAsync(page.Key, request, command.UserId, cancellationToken).ConfigureAwait(false);
            var catalogError = await ValidateCatalogAsync(unitOfWork, request.Placement, request.Icon, request.Url, request.RequiredPermissionKey, cancellationToken)
                .ConfigureAwait(false);
            catalogError ??= await MenuVisibilityValidation.ValidateAsync(unitOfWork, request.RequiredRoleNames, request.RequiredPermissionKeys, cancellationToken).ConfigureAwait(false);
            if (catalogError is not null)
            {
                return AppResponses.Failure<MenuResponse>(catalogError);
            }

            var menu = await unitOfWork.MenuRepository.FindByIdAsync(command.Id, cancellationToken).ConfigureAwait(false);
            if (menu is null)
            {
                return AppResponses.Failure<MenuResponse>($"Menu {command.Id} not found.");
            }

            if (request.ParentId == command.Id)
            {
                return AppResponses.Failure<MenuResponse>("A menu cannot be its own parent.");
            }

            if (request.ParentId.HasValue)
            {
                var parent = await unitOfWork.MenuRepository.FindByIdAsync(request.ParentId.Value, cancellationToken).ConfigureAwait(false);
                if (parent is null)
                {
                    return AppResponses.Failure<MenuResponse>("Parent menu not found.");
                }

                if (!string.Equals(parent.Placement, request.Placement, StringComparison.OrdinalIgnoreCase)
                   )
                {
                    return AppResponses.Failure<MenuResponse>("Parent menu must use the same placement.");
                }
            }


            menu.Update(
                request.ParentId,

                menu.Key, // Prevent key change on update
                request.Title,
                request.Description,
                request.Url,
                request.Icon,
                request.Placement,
                request.RequiredPermissionKey,
                request.RequiredModule,
                request.DisplayOrder,
                request.IsActive,
                command.UserId);
            menu.SetVisibility(request.RequiredRoleNames ?? menu.RequiredRoleNames, request.RequiredPermissionKeys ?? menu.RequiredPermissionKeys);
            await unitOfWork.MenuRepository.UpdateAsync(menu, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved ? AppResponses.Success("Menu updated.", menu.ToMenuResponse()) : AppResponses.Failure<MenuResponse>("Menu update failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(UpdateMenuCommandHandler), ex);
            throw;
        }
    }

    private static async Task<string?> ValidateCatalogAsync(
        IIamUnitOfWork unitOfWork,
        string placement,
        string icon,
        string url,
        string? requiredPermissionKey,
        CancellationToken cancellationToken)
    {
        var normalizedPlacement = placement.Trim();

        var placementExists = await unitOfWork.MenuPlacementRepository
            .AnyAsync(item => item.IsActive && item.Key == normalizedPlacement, cancellationToken)
            .ConfigureAwait(false);

        if (!placementExists)
        {
            return $"Menu placement '{placement}' is not registered.";
        }

        var iconExists = ST.SharedKernel.Features.IAM.Menus.MaterialIconKeys.Contains(icon)
            || await unitOfWork.MenuIconRepository
            .AnyAsync(item => item.IsActive && item.Key == icon.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (!iconExists)
        {
            return $"Menu icon '{icon}' is not registered.";
        }

        var routeExists = string.IsNullOrWhiteSpace(url)
            || await unitOfWork.MenuRouteRepository
                .AnyAsync(item => item.IsActive && item.Url == url.Trim() && item.PlacementKey == normalizedPlacement, cancellationToken)
                .ConfigureAwait(false);

        if (!routeExists)
        {
            return $"Menu route '{url}' is not registered for placement '{placement}'.";
        }

        if (string.IsNullOrWhiteSpace(requiredPermissionKey))
        {
            return null;
        }

        var normalizedPermissionKey = requiredPermissionKey.Trim().ToLowerInvariant();
        var permissionExists = await unitOfWork.PermissionRepository
            .AnyAsync(item => item.IsActive && item.Key == normalizedPermissionKey, cancellationToken)
            .ConfigureAwait(false);

        return permissionExists ? null : $"Required permission '{requiredPermissionKey}' is not registered.";
    }
}
