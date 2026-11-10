using System.Collections.Frozen;
namespace ST.SharedKernel.Features.IAM.Menus;

/// <summary>Material icon key allowlist extracted from MudBlazor 9.8.0. Regenerate on package upgrades.</summary>
public static class MaterialIconKeys
{
    private static readonly FrozenSet<string> Keys = Load();
    public static bool Contains(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var normalized = key.Trim();
        if (!normalized.Contains('.', StringComparison.Ordinal)) normalized = $"Filled.{normalized}";
        return Keys.Contains(normalized);
    }
    private static FrozenSet<string> Load()
    {
        using var stream = typeof(MaterialIconKeys).Assembly.GetManifestResourceStream("ST.MaterialIconKeys")
            ?? throw new InvalidOperationException("Material icon catalogue is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
