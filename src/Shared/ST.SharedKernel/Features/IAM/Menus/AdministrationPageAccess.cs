namespace ST.SharedKernel.Features.IAM.Menus;

/// <summary>Operation floors for shipped administration pages, independent of configurable menu visibility.</summary>
public static class AdministrationPageAccess
{
    public static IReadOnlyList<string> PermissionKeys
    {
        get;
    } =
    [
        "menus.view", "tenant-settings.view", "roles.view", "roles.manage_permissions", "users.view", "support.approve"
    ];

    public static bool CanOpen(string path, IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(permissions);
        var keys = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var route = path.Split('?', '#')[0].TrimEnd('/').ToLowerInvariant();
        return route switch
        {
            "/admin" => PermissionKeys.Any(keys.Contains),
            "/admin/menus" or "/admin/access-catalog" or "/admin/settings" => keys.Contains("menus.view"),
            "/admin/org-settings" => keys.Contains("tenant-settings.view"),
            "/admin/iam" => keys.Contains("roles.view") || keys.Contains("roles.manage_permissions") || keys.Contains("users.view"),
            "/admin/support" => keys.Contains("support.approve"),
            _ => true
        };
    }
}
