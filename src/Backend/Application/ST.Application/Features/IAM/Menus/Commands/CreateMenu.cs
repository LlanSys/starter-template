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



internal sealed class CreateMenuCommandHandler(IIamUnitOfWork unitOfWork, ILogger<CreateMenuCommandHandler> logger, ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy policy, IEffectiveNavigation navigation)
    : IRequestHandler<CreateMenuCommand, AppResponse<MenuResponse>>
{
    public async Task<AppResponse<MenuResponse>> Handle(CreateMenuCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var request = command.Request;
            if (!string.IsNullOrWhiteSpace(request.Url))
            {
                var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
                var page = access.Catalogue.FirstOrDefault(x => x.Kind == ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueKind.Page && x.Path == request.Url && x.Placement == request.Placement);
                if (page is null || !access.EntryAvailable(page)) return AppResponses.Failure<MenuResponse>("Select an available page from the module catalogue.");
                return await navigation.SaveOverrideAsync(page.Key, new UpdateMenuRequest { Id = page.Id, ParentId = request.ParentId, Title = request.Title, Url = request.Url, Icon = request.Icon, Placement = request.Placement, DisplayOrder = request.DisplayOrder, RequiredRoleNames = request.RequiredRoleNames, RequiredPermissionKeys = request.RequiredPermissionKeys }, command.UserId, cancellationToken).ConfigureAwait(false);
            }
            var catalogError = await ValidateCatalogAsync(unitOfWork, request.Placement, request.Icon, request.Url, request.RequiredPermissionKey, cancellationToken)
                .ConfigureAwait(false);
            catalogError ??= await MenuVisibilityValidation.ValidateAsync(unitOfWork, request.RequiredRoleNames, request.RequiredPermissionKeys, cancellationToken).ConfigureAwait(false);
            if (catalogError is not null)
            {
                return AppResponses.Failure<MenuResponse>(catalogError);
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

            var key = Slugify(request.Title);
            var menu = MenuItem.Create(request.ParentId, key, request.Title, request.Description, request.Url, request.Icon, request.Placement, request.RequiredPermissionKey, request.RequiredModule, request.DisplayOrder, command.UserId);
            menu.SetVisibility(request.RequiredRoleNames, request.RequiredPermissionKeys);
            var duplicate = await unitOfWork.MenuRepository
                .AnyAsync(existing => existing.Key == menu.Key, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                return AppResponses.Failure<MenuResponse>($"Menu key {menu.Key} already exists.");
            }

            await unitOfWork.MenuRepository.CreateAsync(menu, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved ? AppResponses.Success("Menu created.", menu.ToMenuResponse()) : AppResponses.Failure<MenuResponse>("Menu create failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(CreateMenuCommandHandler), ex);
            throw;
        }
    }

    private static string Slugify(string value)
        => string.Join('-', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

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
