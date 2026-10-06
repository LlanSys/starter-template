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
        Create(OverviewId, null, null, "solution-overview", "Solution Overview", "Architecture and solution overview.", "/overview", "AutoStories", "Sidebar", null, "Showcase", 1),
        Create(DashboardId, null, null, "dashboard", "Dashboard", "Operations dashboard.", "/dashboard", "Dashboard", "Sidebar", null, "Core", 2),
        Create(AdminId, null, null, "admin-center", "Admin Center", "Administrative workspace.", "/admin", "AdminPanelSettings", "Sidebar", null, "Core", 3),
        Create(ControlPanelId, null, null, "control-panel", "Control Panel", "Platform management.", "/control-panel", "Dns", "Sidebar", "controlplane.manage", null, 4),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c30101", ControlPanelId, null, "control-panel-tenants", "Tenants", "Manage SaaS tenants.", "/control-panel/tenants", "Business", "ControlPanel", "controlplane.manage", null, 10),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c30102", ControlPanelId, null, "control-panel-stamps", "Stamps", "Deployment stamps.", "/control-panel/stamps", "Dns", "ControlPanel", "controlplane.manage", null, 20),

        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c10401", null, null, "features", "Features", "Reusable platform capability showcases.", "/features", "MenuOpen", "Sidebar", null, "Showcase", 4),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c10402", Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"), null, "features-payments", "Payments", "Test card and mobile-money payment flows.", "/features/payments", "CreditCard", "Features", "payments.view", "Showcase", 10),

        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20101", AdminId, null, "admin-customers", "Customers", "Customer records and onboarding.", "/admin/customers", "Business", "AdminCenter", null, "Core", 10),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20102", AdminId, null, "admin-departments", "Departments", "Department catalog and staff grouping.", "/admin/departments", "AccountTree", "AdminCenter", null, "Core", 20),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20103", AdminId, null, "admin-employees", "Employees", "Staff records and system access.", "/admin/employees", "Badge", "AdminCenter", null, "Core", 30),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20104", AdminId, null, "admin-menus", "Menus", "Navigation catalog and menu visibility.", "/admin/menus", "MenuOpen", "AdminCenter", null, "Core", 40),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20107", AdminId, null, "admin-org-settings", "Org Settings", "Tenant-specific configuration surface.", "/admin/org-settings", "Settings", "AdminCenter", null, "Core", 50),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20111", AdminId, null, "admin-iam", "Identity & Access", "Manage users, roles, permissions, and trusted devices.", "/admin/iam", "Group", "AdminCenter", null, "IAM", 60),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20110", AdminId, null, "admin-access-catalog", "Access Catalog", "Source-of-truth permission and menu reference data.", "/admin/access-catalog", "LockPerson", "AdminCenter", null, "IAM", 70)
    ];

    private static MenuItem Create(string id, Guid? parentId, Guid? departmentId, string key, string title, string description, string url, string icon, string placement, string? requiredPermissionKey, string? requiredModule, int displayOrder)
        => Create(Guid.Parse(id), parentId, departmentId, key, title, description, url, icon, placement, requiredPermissionKey, requiredModule, displayOrder);

    private static MenuItem Create(Guid id, Guid? parentId, Guid? departmentId, string key, string title, string description, string url, string icon, string placement, string? requiredPermissionKey, string? requiredModule, int displayOrder)
    {
        var menu = MenuItem.Create(parentId, departmentId, key, title, description, url, icon, placement, requiredPermissionKey, requiredModule, displayOrder, "System");
        menu.Id = id;
        menu.TenantId = SeedTenantId;
        menu.CreatedAt = SeedCreatedAt;
        return menu;
    }
}
