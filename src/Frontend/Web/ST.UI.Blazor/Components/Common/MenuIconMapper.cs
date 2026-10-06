using MudBlazor;
using System.Reflection;

namespace ST.UI.Blazor.Components.Common;

internal static class MenuIconMapper
{
    private static readonly Lazy<Dictionary<string, string>> MaterialFilledIcons = new(BuildMaterialFilledIconMap);

    public static IReadOnlyList<MenuIconOption> Options { get; } =
    [
        new("AccountTree", "Account tree", Icons.Material.Filled.AccountTree),
        new("AdminPanelSettings", "Admin panel", Icons.Material.Filled.AdminPanelSettings),
        new("AutoStories", "Story/book", Icons.Material.Filled.AutoStories),
        new("Badge", "Badge", Icons.Material.Filled.Badge),
        new("Business", "Business", Icons.Material.Filled.Business),
        new("Dashboard", "Dashboard", Icons.Material.Filled.Dashboard),
        new("Devices", "Devices", Icons.Material.Filled.Devices),
        new("Group", "Group", Icons.Material.Filled.Group),
        new("LockPerson", "Security lock", Icons.Material.Filled.LockPerson),
        new("Menu", "Generic menu", Icons.Material.Filled.Menu),
        new("MenuOpen", "Menu", Icons.Material.Filled.MenuOpen),
        new("Settings", "Settings", Icons.Material.Filled.Settings)
    ];

    public static string Resolve(string? icon)
    {
        var key = icon?.Trim();
        return string.IsNullOrWhiteSpace(key)
            ? Icons.Material.Filled.Menu
            : Options.FirstOrDefault(option => option.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Icon
            ?? (MaterialFilledIcons.Value.TryGetValue(key, out var resolvedIcon) ? resolvedIcon : Icons.Material.Filled.Menu);
    }

    private static Dictionary<string, string> BuildMaterialFilledIconMap()
    {
        return typeof(Icons.Material.Filled)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(static property => property.PropertyType == typeof(string))
            .Select(static property => new
            {
                property.Name,
                Icon = property.GetValue(null) as string
            })
            .Where(static item => !string.IsNullOrWhiteSpace(item.Icon))
            .ToDictionary(static item => item.Name, static item => item.Icon!, StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed record MenuIconOption(string Key, string Label, string Icon);
