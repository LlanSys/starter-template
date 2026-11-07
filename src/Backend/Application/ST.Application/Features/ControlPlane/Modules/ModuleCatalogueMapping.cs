using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
public static class ModuleCatalogueMapping
{
    public static string PermissionContext(ModuleCatalogueEntry entry) => entry.ModuleKey.Equals("payments", StringComparison.OrdinalIgnoreCase) ? "shared" : entry.Key.StartsWith("menus.", StringComparison.OrdinalIgnoreCase) ? "platform" : "iam";
    public static ST.SharedKernel.Features.IAM.Permissions.Dtos.PermissionResponse ToPermissionResponse(this ModuleCatalogueEntry x) => new(x.Id, x.Key, PermissionContext(x), x.Key.Contains('.') ? x.Key[..x.Key.LastIndexOf('.')] : x.Key, x.Key.Contains('.') ? x.Key[(x.Key.LastIndexOf('.') + 1)..] : "", x.Title, x.IsActive) { ModuleKey = x.ModuleKey, IsStandard = true };
    public static CatalogueEntryResponse ToResponse(this ModuleCatalogueEntry x) => new(x.Id, x.ModuleKey, x.Key, x.Kind.ToString(), x.Title, x.Path, x.ParentKey, x.Icon, x.Placement, x.DisplayOrder, x.ShowInNavigation, x.PermissionKeys, x.RoleNames, x.FeatureFlag, x.IsActive, x.Kind != ModuleCatalogueKind.Page ? ModuleImplementations.Contains(x.ModuleKey) : (string.IsNullOrEmpty(x.Path) || ModulePageBindings.IsImplemented(x.Path, x.ModuleKey)), x.Version);
}
