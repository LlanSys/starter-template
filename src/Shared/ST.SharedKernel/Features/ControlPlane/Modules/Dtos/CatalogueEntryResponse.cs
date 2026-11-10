namespace ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
public sealed record CatalogueEntryResponse(Guid Id, string ModuleKey, string Key, string Kind, string Title, string Path, string? ParentKey,
    string Icon, string Placement, int DisplayOrder, bool ShowInNavigation, string[] PermissionKeys, string[] RoleNames, string? FeatureFlag, bool IsActive, bool IsImplemented, int Version);
