using ST.Domain.Features.ControlPlane.Modules.Entities;
namespace ST.Application.Features.ControlPlane.Modules;
public sealed class EffectiveModuleAccess
{
    public required IReadOnlySet<string> Modules { get; init; }
    public required IReadOnlyList<ModuleCatalogueEntry> Catalogue { get; init; }
    public IReadOnlyDictionary<string, string> PrivatePermissionModules { get; init; } = new Dictionary<string, string>();
    public IReadOnlySet<string> InactivePrivatePermissions { get; init; } = new HashSet<string>();
    public required IReadOnlySet<string> EnabledFlags { get; init; }
    public bool FlagEnabled(string module, string? flag) => string.IsNullOrWhiteSpace(flag) || EnabledFlags.Contains($"{module}:{flag}");
    public bool EntryAvailable(ModuleCatalogueEntry entry) => entry.IsActive && Modules.Contains(entry.ModuleKey) && FlagEnabled(entry.ModuleKey, entry.FeatureFlag)
        && (entry.Kind != ModuleCatalogueKind.RoleTemplate || entry.PermissionKeys.All(PermissionAvailable))
        && (entry.Kind != ModuleCatalogueKind.Page || (string.IsNullOrEmpty(entry.Path) || (ModulePageBindings.IsImplemented(entry.Path, entry.ModuleKey)
            && !Catalogue.Any(other => other.Kind == ModuleCatalogueKind.Page && other.Path == entry.Path && (!other.IsActive || !FlagEnabled(other.ModuleKey, other.FeatureFlag))))));
    public bool PermissionAvailable(string key)
    {
        if (key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase)) return false;
        var entry = Catalogue.FirstOrDefault(x => x.Kind == ModuleCatalogueKind.Permission && string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (entry is not null) return EntryAvailable(entry);
        if (InactivePrivatePermissions.Contains(key)) return false;
        return Modules.Contains(PrivatePermissionModules.TryGetValue(key, out var owner) ? owner : key.StartsWith("payments.", StringComparison.OrdinalIgnoreCase) ? "payments" : "iam");
    }
    public IReadOnlyList<ModuleCatalogueEntry> AvailablePages => Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Page && EntryAvailable(x)).ToArray();
}
