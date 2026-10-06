using ST.Domain.Features.IAM.ReferenceData.Entities;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;

namespace ST.Application.Features.IAM.ReferenceData.Mappings;

internal static class ReferenceCatalogMapping
{
    internal static ReferenceCatalogItemResponse ToResponse(this PermissionContext item)
        => new(item.Id, ReferenceCatalogTypes.PermissionContexts, item.Key, item.Label, item.Description, null, null, item.IsActive);

    internal static ReferenceCatalogItemResponse ToResponse(this PermissionResource item)
        => new(item.Id, ReferenceCatalogTypes.PermissionResources, item.Key, item.Label, item.Description, item.ContextKey, null, item.IsActive);

    internal static ReferenceCatalogItemResponse ToResponse(this PermissionAction item)
        => new(item.Id, ReferenceCatalogTypes.PermissionActions, item.Key, item.Label, item.Description, null, null, item.IsActive);

    internal static ReferenceCatalogItemResponse ToResponse(this MenuPlacement item)
        => new(item.Id, ReferenceCatalogTypes.MenuPlacements, item.Key, item.Label, item.Description, null, null, item.IsActive);

    internal static ReferenceCatalogItemResponse ToResponse(this MenuIcon item)
        => new(item.Id, ReferenceCatalogTypes.MenuIcons, item.Key, item.Label, item.Description, null, null, item.IsActive);

    internal static ReferenceCatalogItemResponse ToResponse(this MenuRoute item)
        => new(item.Id, ReferenceCatalogTypes.MenuRoutes, item.Key, item.Label, item.Description, item.PlacementKey, item.Url, item.IsActive);
}
