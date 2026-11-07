using ST.Domain.Features.IAM.ReferenceData.Entities;

namespace ST.Persistence.Features.IAM.ReferenceData.Seeds;

internal static class IamReferenceDataSeed
{
    private static readonly DateTimeOffset SeedCreatedAt = new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly Guid SeedTenantId = new("0194f700-0000-7000-8000-000000000001");

    internal static IReadOnlyList<PermissionContext> PermissionContexts =>
    [


        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10103", "IAM", "Identity and Access", "Users, roles, permissions, sessions, and devices."),
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10104", "Platform", "Platform", "Cross-cutting platform configuration and navigation."),
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10106", "ControlPlane", "Control Plane", "Tenant approval and deployment administration."),
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10105", "Shared", "Shared", "Cross-cutting platform services and reusable capability showcases.")
    ];

    internal static IReadOnlyList<PermissionResource> PermissionResources =>
    [



        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10204", "menus", "Menus", "Platform", "Navigation registry and menu visibility."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10205", "permissions", "Permissions", "IAM", "Permission catalog and assignment surface."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10206", "roles", "Roles", "IAM", "Role catalog and permission bundles."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10207", "users", "Users", "IAM", "Application user accounts."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10209", "control_plane", "Control Plane", "ControlPlane", "Tenant and stamp administration."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10208", "payments", "Payments", "Shared", "Payment checkout, status, and provider administration." )
    ];

    internal static IReadOnlyList<PermissionAction> PermissionActions =>
    [
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10301", "view", "View", "Read and list records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10302", "create", "Create", "Create new records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10303", "edit", "Edit", "Update existing records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10304", "delete", "Delete", "Soft-delete or remove records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10305", "deactivate", "Deactivate", "Disable active records or accounts."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10306", "manage_permissions", "Manage permissions", "Assign or revoke permissions."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10307", "manage_roles", "Manage roles", "Assign or revoke roles."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10309", "manage", "Manage", "Manage a platform capability."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10308", "admin", "Administer", "Perform restricted provider administration actions.")
    ];

    internal static IReadOnlyList<MenuPlacement> MenuPlacements =>
    [
        CreatePlacement("018fd81d-2c94-7ad0-a4a3-f1edb9d10401", "Sidebar", "Sidebar", "Main application navigation."),
        CreatePlacement("018fd81d-2c94-7ad0-a4a3-f1edb9d10402", "AdminCenter", "Admin Center", "Administration landing tiles."),
        CreatePlacement("018fd81d-2c94-7ad0-a4a3-f1edb9d10403", "ControlPanel", "Control Panel", "Platform control plane tiles.")
    ];

    internal static IReadOnlyList<MenuIcon> MenuIcons =>
    [
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10501", "AccountTree", "Account tree"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10502", "AdminPanelSettings", "Admin panel"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10503", "AutoStories", "Story/book"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10504", "Badge", "Badge"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10505", "Business", "Business"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10506", "Dashboard", "Dashboard"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10507", "Devices", "Devices"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10508", "Group", "Group"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10509", "LockPerson", "Security lock"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10510", "Menu", "Generic menu"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10511", "MenuOpen", "Menu"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10512", "Settings", "Settings"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10513", "CreditCard", "Credit card"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10514", "Dns", "DNS/platform")
    ];

    internal static IReadOnlyList<MenuRoute> MenuRoutes =>
    [
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10601", "dashboard", "Dashboard", "/dashboard", "Sidebar"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10602", "admin-center", "Admin Center", "/admin", "Sidebar"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10603", "solution-overview", "Solution Overview", "/overview", "Sidebar"),



        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10607", "admin-menus", "Menus", "/admin/menus", "AdminCenter"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10608", "admin-permissions", "Permissions", "/admin/permissions", "AdminCenter"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10609", "admin-roles", "Roles", "/admin/roles", "AdminCenter"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10610", "admin-settings", "Settings", "/admin/settings", "AdminCenter"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10611", "admin-user-devices", "User Devices", "/admin/user-devices", "AdminCenter"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10612", "admin-users", "Users", "/admin/users", "AdminCenter"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10613", "admin-access-catalog", "Access Catalog", "/admin/access-catalog", "AdminCenter"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10614", "features", "Features", "/features", "Sidebar"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10615", "features-payments", "Payments", "/payments", "Sidebar"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10616", "control-panel", "Control Panel", "/control-panel", "Sidebar"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10617", "control-panel-tenants", "Tenants", "/control-panel/tenants", "ControlPanel"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10618", "control-panel-stamps", "Stamps", "/control-panel/stamps", "ControlPanel"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10619", "admin-org-settings", "Org Settings", "/admin/org-settings", "AdminCenter"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10620", "admin-iam", "Identity & Access", "/admin/iam", "AdminCenter")
    ];

    private static PermissionContext CreateContext(string id, string key, string label, string description)
    {
        var item = PermissionContext.Create(key, label, description, "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static PermissionResource CreateResource(string id, string key, string label, string contextKey, string description)
    {
        var item = PermissionResource.Create(key, label, contextKey, description, "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static PermissionAction CreateAction(string id, string key, string label, string description)
    {
        var item = PermissionAction.Create(key, label, description, "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static MenuPlacement CreatePlacement(string id, string key, string label, string description)
    {
        var item = MenuPlacement.Create(key, label, description, "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static MenuIcon CreateIcon(string id, string key, string label)
    {
        var item = MenuIcon.Create(key, label, "Approved MudBlazor icon key.", "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static MenuRoute CreateRoute(string id, string key, string label, string url, string placementKey)
    {
        var item = MenuRoute.Create(key, label, url, placementKey, "Approved application route.", "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }
}
