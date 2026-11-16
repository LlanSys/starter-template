using System.Collections.Frozen;

namespace ST.UI.Blazor.Components.Layout;

public static class TemplateNavigation
{
    private static readonly FrozenSet<string> RetiredPaths = new[] { "/overview", "/features", "/counter", "/weather" }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsRetired(string? url)
    {
        return url is not null && RetiredPaths.Contains(url.TrimEnd('/'));
    }

    public static bool IsRetiredKey(string? key)
    {
        return key is not null
            && (key.Equals("solution-overview", StringComparison.OrdinalIgnoreCase) || IsRetired("/" + key));
    }

    public static bool IsRetiredModule(string? key)
    {
        return string.Equals(key, "Showcase", StringComparison.OrdinalIgnoreCase);
    }
}
