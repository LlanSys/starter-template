using MudBlazor;
using System.Collections.Frozen;
using System.Reflection;
namespace ST.UI.Blazor.Components.Common;

internal static class MenuIconMapper
{
    private static readonly FrozenDictionary<string, string> IconsByKey = typeof(Icons.Material)
        .GetNestedTypes(BindingFlags.Public)
        .SelectMany(style => style.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => KeyValuePair.Create($"{style.Name}.{field.Name}", (string)field.GetValue(null)!)))
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] Keys = IconsByKey.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    public static string Resolve(string? icon)
    {
        var key = icon?.Trim() ?? string.Empty;
        if (!key.Contains('.', StringComparison.Ordinal)) key = $"Filled.{key}";
        return IconsByKey.GetValueOrDefault(key, Icons.Material.Filled.Menu);
    }
    public static Task<IEnumerable<string>> SearchAsync(string? text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var terms = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Task.FromResult<IEnumerable<string>>(Keys.Where(key => terms.All(term => key.Contains(term, StringComparison.OrdinalIgnoreCase))).ToArray());
    }
}
