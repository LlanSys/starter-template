using ST.Domain.Features.IAM.Permissions.Entities;

namespace ST.Persistence.Features.IAM.Permissions.Seeds;

internal static class PermissionSeed
{
    private static readonly DateTimeOffset SeedCreatedAt = new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly Guid SeedTenantId = new("0194f700-0000-7000-8000-000000000001");

    internal static IReadOnlyList<Permission> Items =>
    [
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10101", "IAM", "users", "view", "View application users."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10102", "IAM", "users", "create", "Create application users."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10103", "IAM", "users", "edit", "Update application users."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10104", "IAM", "users", "deactivate", "Deactivate application users."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10105", "IAM", "users", "manage_roles", "Manage user role assignments."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10106", "IAM", "users", "manage_permissions", "Manage direct user permissions."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10201", "IAM", "roles", "view", "View platform roles."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10202", "IAM", "roles", "create", "Create platform roles."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10203", "IAM", "roles", "edit", "Update platform roles."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10204", "IAM", "roles", "delete", "Delete platform roles."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10205", "IAM", "roles", "manage_permissions", "Manage role permission assignments."),












        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10601", "Platform", "menus", "view", "View menu catalog."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10602", "Platform", "menus", "create", "Create menu items."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10603", "Platform", "menus", "edit", "Update menu items."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10604", "Platform", "menus", "delete", "Delete menu items."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10701", "Shared", "payments", "view", "View payment history and provider readiness."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10702", "Shared", "payments", "create", "Initiate payment checkout flows."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10703", "Shared", "payments", "admin", "Manage M-Pesa payment administration actions."),
        Create("0194f700-0000-7000-8000-000000000092", "Shared", "tasks", "assign", "Assign operational tasks within this workspace."),
        .. PlatformItems
    ];

    private static IReadOnlyList<Permission> PlatformItems =>
    [
        Create("0194f700-0000-7000-8000-000000000096", "IAM", "tenant-settings", "view", "View workspace organization settings."),
        Create("0194f700-0000-7000-8000-000000000097", "IAM", "tenant-settings", "create", "Create workspace organization settings."),
        Create("0194f700-0000-7000-8000-000000000098", "IAM", "tenant-settings", "update", "Update workspace organization settings."),
        Create("0194f700-0000-7000-8000-000000000099", "IAM", "tenant-settings", "delete", "Delete workspace organization settings."),
        Create("0194f700-0000-7000-8000-000000000094", "ControlPlane", "control_plane", "view_platform_staff", "View platform staff and their access history."),
        Create("0194f700-0000-7000-8000-000000000095", "ControlPlane", "control_plane", "manage_platform_staff", "Manage platform staff membership and operation grants."),
        Create("0194f700-0000-7000-8000-000000000093", "ControlPlane", "control_plane", "manage_tasks", "Assign platform operational tasks."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12001", "ControlPlane", "control_plane", "view_tenants", "Platform operation: view tenants."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12002", "ControlPlane", "control_plane", "create_tenants", "Platform operation: create tenants."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12003", "ControlPlane", "control_plane", "edit_tenants", "Platform operation: edit tenants."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12004", "ControlPlane", "control_plane", "manage_tenant_administrators", "Platform operation: manage tenant administrators."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12005", "ControlPlane", "control_plane", "manage_tenant_lifecycle", "Platform operation: manage tenant lifecycle."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12006", "ControlPlane", "control_plane", "manage_tenant_modules", "Platform operation: manage tenant modules."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12007", "ControlPlane", "control_plane", "migrate_tenants", "Platform operation: migrate tenants."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12008", "ControlPlane", "control_plane", "view_stamps", "Platform operation: view stamps."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12009", "ControlPlane", "control_plane", "manage_stamps", "Platform operation: manage stamps."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12010", "ControlPlane", "control_plane", "view_modules", "Platform operation: view modules."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12011", "ControlPlane", "control_plane", "manage_modules", "Platform operation: manage modules."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12012", "ControlPlane", "control_plane", "view_audit", "Platform operation: view audit."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12013", "ControlPlane", "control_plane", "view_support", "Platform operation: view support."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12014", "ControlPlane", "control_plane", "manage_support", "Platform operation: manage support."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12015", "ControlPlane", "control_plane", "enter_demo", "Platform operation: enter demo."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12016", "ControlPlane", "control_plane", "reset_demo", "Platform operation: reset demo."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12017", "ControlPlane", "control_plane", "view_kyc", "Platform operation: view kyc."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12018", "ControlPlane", "control_plane", "upload_kyc_documents", "Platform operation: upload kyc documents."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12019", "ControlPlane", "control_plane", "download_kyc_documents", "Platform operation: download kyc documents."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12020", "ControlPlane", "control_plane", "review_kyc_documents", "Platform operation: review kyc documents."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12021", "ControlPlane", "control_plane", "manage_kyc_submissions", "Platform operation: manage kyc submissions."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12022", "ControlPlane", "control_plane", "approve_kyc", "Platform operation: approve kyc."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12023", "ControlPlane", "control_plane", "manage_kyc_retention", "Platform operation: manage kyc retention."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b12024", "ControlPlane", "control_plane", "manage_kyc_holds", "Platform operation: manage kyc holds."),
    ];

    private static Permission Create(string id, string context, string resource, string action, string description)
    {
        var permission = Permission.Create(context, resource, action, description, context == "ControlPlane" ? "00000000-0000-0000-0000-000000000001" : "System");
        permission.SetModule(resource == "payments" ? "payments" : "iam");
        permission.Id = Guid.Parse(id);
        permission.TenantId = SeedTenantId;
        permission.CreatedAt = SeedCreatedAt;
        return permission;
    }
}
