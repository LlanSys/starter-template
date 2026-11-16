using System.Globalization;

namespace ST.UI.Blazor.Components.Common;

public static class UiLabels
{
    public static string TitleCase(string? label)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(label ?? string.Empty);
    }
}
