namespace ST.SharedKernel.Features.IAM.ReferenceData.Dtos;

public sealed record IamReferenceDataResponse(
    IReadOnlyList<CatalogOptionResponse> PermissionContexts,
    IReadOnlyList<CatalogOptionResponse> PermissionResources,
    IReadOnlyList<CatalogOptionResponse> PermissionActions,
    IReadOnlyList<CatalogOptionResponse> MenuPlacements,
    IReadOnlyList<CatalogOptionResponse> MenuIcons,
    IReadOnlyList<CatalogOptionResponse> MenuRoutes);
