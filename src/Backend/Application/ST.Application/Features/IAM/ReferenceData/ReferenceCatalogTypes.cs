namespace ST.Application.Features.IAM.ReferenceData;

internal static class ReferenceCatalogTypes
{
    internal const string PermissionContexts = "permission-contexts";
    internal const string PermissionResources = "permission-resources";
    internal const string PermissionActions = "permission-actions";
    internal const string MenuPlacements = "menu-placements";
    internal const string MenuIcons = "menu-icons";
    internal const string MenuRoutes = "menu-routes";

    internal static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PermissionContexts,
        PermissionResources,
        PermissionActions,
        MenuPlacements,
        MenuIcons,
        MenuRoutes
    };
}
