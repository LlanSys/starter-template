namespace ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
public sealed record SaveCatalogueEntryRequest(string Key, string Kind, string Title, string Path = "", string? ParentKey = null,
    string Icon = "Menu", string Placement = "Sidebar", int DisplayOrder = 0, bool ShowInNavigation = true,
    string[]? PermissionKeys = null, string[]? RoleNames = null, string? FeatureFlag = null, bool IsActive = true, int? ExpectedVersion = null);
