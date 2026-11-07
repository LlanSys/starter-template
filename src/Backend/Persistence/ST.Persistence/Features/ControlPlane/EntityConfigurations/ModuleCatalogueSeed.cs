using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Persistence.Features.IAM.Menus.Seeds;
using ST.Persistence.Features.IAM.Permissions.Seeds;
namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;
internal static class ModuleCatalogueSeed
{
    public static IReadOnlyList<ModuleCatalogueEntry> Entries
    {
        get
        {
            var menus = MenuItemSeed.Items.Where(x => x.RequiredPermissionKey != "control_plane.manage" && x.Placement != "ControlPanel").ToArray();
            var entries = menus.Select(x => new ModuleCatalogueEntry
            {
                Id = x.Id, Key = x.Key, ModuleKey = x.Url is "/admin/menus" or "/admin/access-catalog" or "/admin/iam" ? "iam" : x.RequiredModule ?? "core", Title = x.Key == "admin-menus" ? "Navigation" : x.Title,
                Kind = ModuleCatalogueKind.Page, Path = x.Url, Icon = x.Icon, Placement = x.Placement,
                ParentKey = x.ParentId is { } parent ? menus.FirstOrDefault(y => y.Id == parent)?.Key : null,
                DisplayOrder = x.DisplayOrder, ShowInNavigation = true,
                PermissionKeys = x.RequiredPermissionKey is { } permission ? [permission] : [],
                CreatedBy = "System", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }).ToList();
            entries.AddRange(PermissionSeed.Items.Where(x => x.Key != "control_plane.manage").Select(x => new ModuleCatalogueEntry
            {
                Id = x.Id, Key = x.Key, Title = x.Description, Kind = ModuleCatalogueKind.Permission,
                ModuleKey = x.Resource == "payments" ? "payments" : "iam", ShowInNavigation = false,
                CreatedBy = "System", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }));
            entries.Add(new ModuleCatalogueEntry { Id=Guid.Parse("0194f700-0000-7000-8000-000000000024"),ModuleKey="iam",Key="support-access",Title="Support access",Kind=ModuleCatalogueKind.Page,Path="/admin/support",Placement="AdminCenter",ParentKey="admin-center",DisplayOrder=90,Icon="SupportAgent",PermissionKeys=["support.approve"],CreatedBy="System",CreatedAt=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero) });
            entries.Add(new ModuleCatalogueEntry { Id=Guid.Parse("0194f700-0000-7000-8000-000000000023"),ModuleKey="iam",Key="support.approve",Title="Approve and revoke customer support",Kind=ModuleCatalogueKind.Permission,ShowInNavigation=false,CreatedBy="System",CreatedAt=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero) });
            entries.Add(new ModuleCatalogueEntry { Id = Guid.Parse("0194f700-0000-7000-8000-000000000021"), ModuleKey = "iam", Key = "iam-administrator", Title = "Tenant Administrator", Kind = ModuleCatalogueKind.RoleTemplate, ShowInNavigation = false, PermissionKeys = entries.Where(x => x.Kind == ModuleCatalogueKind.Permission && x.ModuleKey == "iam").Select(x => x.Key).ToArray(), CreatedBy = "System", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) });
            entries.Add(new ModuleCatalogueEntry { Id = Guid.Parse("0194f700-0000-7000-8000-000000000022"), ModuleKey = "payments", Key = "payments-viewer", Title = "Payments Viewer", Kind = ModuleCatalogueKind.RoleTemplate, ShowInNavigation = false, PermissionKeys = ["payments.view"], CreatedBy = "System", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) });
            return entries;
        }
    }
}
