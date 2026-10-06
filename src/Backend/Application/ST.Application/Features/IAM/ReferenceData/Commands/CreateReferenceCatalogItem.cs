using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.ReferenceData.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.ReferenceData.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.ReferenceData.Commands;



internal sealed class CreateReferenceCatalogItemCommandHandler(IIamUnitOfWork unitOfWork, ILogger<CreateReferenceCatalogItemCommandHandler> logger)
    : IRequestHandler<CreateReferenceCatalogItemCommand, AppResponse<ReferenceCatalogItemResponse>>
{
    public async Task<AppResponse<ReferenceCatalogItemResponse>> Handle(CreateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (!ReferenceCatalogTypes.All.Contains(command.CatalogType))
            {
                return AppResponses.Failure<ReferenceCatalogItemResponse>($"Catalog '{command.CatalogType}' is not supported.");
            }

            var duplicate = await KeyExistsAsync(unitOfWork, command.CatalogType, command.Request.Key, command.Request.ParentKey, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate)
            {
                return AppResponses.Failure<ReferenceCatalogItemResponse>($"Catalog key '{command.Request.Key}' already exists.");
            }

            var validationError = await ValidateParentAsync(unitOfWork, command.CatalogType, command.Request, cancellationToken).ConfigureAwait(false);
            if (validationError is not null)
            {
                return AppResponses.Failure<ReferenceCatalogItemResponse>(validationError);
            }

            ReferenceCatalogItemResponse response = command.CatalogType.ToLowerInvariant() switch
            {
                ReferenceCatalogTypes.PermissionContexts => await CreatePermissionContextAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.PermissionResources => await CreatePermissionResourceAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.PermissionActions => await CreatePermissionActionAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.MenuPlacements => await CreateMenuPlacementAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.MenuIcons => await CreateMenuIconAsync(command, cancellationToken).ConfigureAwait(false),
                ReferenceCatalogTypes.MenuRoutes => await CreateMenuRouteAsync(command, cancellationToken).ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Unsupported catalog {command.CatalogType}.")
            };

            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;
            return saved
                ? AppResponses.Success("Reference catalog item created.", response)
                : AppResponses.Failure<ReferenceCatalogItemResponse>("Reference catalog item create failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(CreateReferenceCatalogItemCommandHandler), ex);
            throw;
        }
    }

    private async Task<ReferenceCatalogItemResponse> CreatePermissionContextAsync(CreateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = PermissionContext.Create(command.Request.Key, command.Request.Label, command.Request.Description, command.UserId);
        await unitOfWork.PermissionContextRepository.CreateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse> CreatePermissionResourceAsync(CreateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = PermissionResource.Create(command.Request.Key, command.Request.Label, command.Request.ParentKey ?? string.Empty, command.Request.Description, command.UserId);
        await unitOfWork.PermissionResourceRepository.CreateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse> CreatePermissionActionAsync(CreateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = PermissionAction.Create(command.Request.Key, command.Request.Label, command.Request.Description, command.UserId);
        await unitOfWork.PermissionActionRepository.CreateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse> CreateMenuPlacementAsync(CreateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = MenuPlacement.Create(command.Request.Key, command.Request.Label, command.Request.Description, command.UserId);
        await unitOfWork.MenuPlacementRepository.CreateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse> CreateMenuIconAsync(CreateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = MenuIcon.Create(command.Request.Key, command.Request.Label, command.Request.Description, command.UserId);
        await unitOfWork.MenuIconRepository.CreateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private async Task<ReferenceCatalogItemResponse> CreateMenuRouteAsync(CreateReferenceCatalogItemCommand command, CancellationToken cancellationToken)
    {
        var item = MenuRoute.Create(command.Request.Key, command.Request.Label, command.Request.Url ?? string.Empty, command.Request.ParentKey ?? string.Empty, command.Request.Description, command.UserId);
        await unitOfWork.MenuRouteRepository.CreateAsync(item, cancellationToken).ConfigureAwait(false);
        return item.ToResponse();
    }

    private static async Task<bool> KeyExistsAsync(IIamUnitOfWork unitOfWork, string catalogType, string key, string? parentKey, CancellationToken cancellationToken)
    {
        var normalizedKey = key.Trim();
        return catalogType.ToLowerInvariant() switch
        {
            ReferenceCatalogTypes.PermissionContexts => await unitOfWork.PermissionContextRepository.AnyAsync(item => item.Key == normalizedKey, cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.PermissionResources => await unitOfWork.PermissionResourceRepository.AnyAsync(item => item.ContextKey == (parentKey ?? string.Empty).Trim() && item.Key == normalizedKey.ToLowerInvariant().Replace(' ', '_'), cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.PermissionActions => await unitOfWork.PermissionActionRepository.AnyAsync(item => item.Key == normalizedKey.ToLowerInvariant().Replace(' ', '_'), cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.MenuPlacements => await unitOfWork.MenuPlacementRepository.AnyAsync(item => item.Key == normalizedKey, cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.MenuIcons => await unitOfWork.MenuIconRepository.AnyAsync(item => item.Key == normalizedKey, cancellationToken).ConfigureAwait(false),
            ReferenceCatalogTypes.MenuRoutes => await unitOfWork.MenuRouteRepository.AnyAsync(item => item.Key == normalizedKey.ToLowerInvariant().Replace(' ', '-'), cancellationToken).ConfigureAwait(false),
            _ => false
        };
    }

    private static async Task<string?> ValidateParentAsync(IIamUnitOfWork unitOfWork, string catalogType, ReferenceCatalogItemRequest request, CancellationToken cancellationToken)
    {
        if (catalogType.Equals(ReferenceCatalogTypes.PermissionResources, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.ParentKey))
            {
                return "Permission resource requires a permission context.";
            }

            var contextExists = await unitOfWork.PermissionContextRepository
                .AnyAsync(item => item.IsActive && item.Key == request.ParentKey.Trim(), cancellationToken)
                .ConfigureAwait(false);

            return contextExists ? null : $"Permission context '{request.ParentKey}' is not registered.";
        }

        if (catalogType.Equals(ReferenceCatalogTypes.MenuRoutes, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.ParentKey))
            {
                return "Menu route requires a placement.";
            }

            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return "Menu route requires a URL.";
            }

            var placementExists = await unitOfWork.MenuPlacementRepository
                .AnyAsync(item => item.IsActive && item.Key == request.ParentKey.Trim(), cancellationToken)
                .ConfigureAwait(false);

            return placementExists ? null : $"Menu placement '{request.ParentKey}' is not registered.";
        }

        return null;
    }
}
