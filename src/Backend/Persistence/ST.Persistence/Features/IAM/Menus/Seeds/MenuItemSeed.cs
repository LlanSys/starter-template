using ST.Domain.Features.IAM.Menus.Entities;

namespace ST.Persistence.Features.IAM.Menus.Seeds;

internal static class MenuItemSeed
{
    private static readonly DateTimeOffset SeedCreatedAt = new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly Guid SeedTenantId = new("0194f700-0000-7000-8000-000000000001");

    private static readonly Guid DashboardId = Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c10101");
    private static readonly Guid AdminId = Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c10201");
    private static readonly Guid ControlPanelId = Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c10501");
    private static readonly Guid OverviewId = Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c10301");

    internal static IReadOnlyList<MenuItem> Items =>
    [
        Create(OverviewId, null, "solution-overview", "Solution Overview", "Architecture and solution overview.", "/overview", "AutoStories", "Sidebar", null, "Showcase", 1),
        Create(DashboardId, null, "dashboard", "Dashboard", "Operations dashboard.", "/dashboard", "Dashboard", "Sidebar", null, "Core", 2),
        Create(AdminId, null, "admin-center", "Admin Center", "Administrative workspace.", "/admin", "AdminPanelSettings", "Sidebar", null, "Core", 3),
        Create(ControlPanelId, null, "control-panel", "Control Panel", "Platform management.", "/control-panel", "Dns", "Sidebar", "control_plane.manage", null, 4),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c30101", ControlPanelId, "control-panel-tenants", "Tenants", "Manage SaaS tenants.", "/control-panel/tenants", "Business", "ControlPanel", "control_plane.manage", null, 10),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c30102", ControlPanelId, "control-panel-stamps", "Stamps", "Deployment stamps.", "/control-panel/stamps", "Dns", "ControlPanel", "control_plane.manage", null, 20),

        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c10401", null, "features", "Payments", "Payment history and checkout.", "/payments", "CreditCard", "Sidebar", "payments.view", "Payments", 4),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c10402", Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"), "features-payments", "Payments", "Test card and mobile-money payment flows.", "/payments", "CreditCard", "Features", "payments.view", "Payments", 10),



        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20104", AdminId, "admin-menus", "Menus", "Navigation catalog and menu visibility.", "/admin/menus", "MenuOpen", "AdminCenter", null, "Core", 40),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20107", AdminId, "admin-org-settings", "Org Settings", "Tenant-specific configuration surface.", "/admin/org-settings", "Settings", "AdminCenter", null, "Core", 50),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20111", AdminId, "admin-iam", "Identity & Access", "Manage users, roles, permissions, and trusted devices.", "/admin/iam", "Group", "AdminCenter", null, "IAM", 60),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20110", AdminId, "admin-access-catalog", "Access Catalog", "Source-of-truth permission and menu reference data.", "/admin/access-catalog", "LockPerson", "AdminCenter", null, "IAM", 70)
    ];

    private static MenuItem Create(string id, Guid? parentId, string key, string title, string description, string url, string icon, string placement, string? requiredPermissionKey, string? requiredModule, int displayOrder)
        => Create(Guid.Parse(id), parentId, key, title, description, url, icon, placement, requiredPermissionKey, requiredModule, displayOrder);

    private static MenuItem Create(Guid id, Guid? parentId, string key, string title, string description, string url, string icon, string placement, string? requiredPermissionKey, string? requiredModule, int displayOrder)
    {
        var menu = MenuItem.Create(parentId, key, title, description, url, icon, placement, requiredPermissionKey, requiredModule, displayOrder, "System");
        menu.Id = id;
        menu.TenantId = SeedTenantId;
        menu.CreatedAt = SeedCreatedAt;
        return menu;
    }
}
