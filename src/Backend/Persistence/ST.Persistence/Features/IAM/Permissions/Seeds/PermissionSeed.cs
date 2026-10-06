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
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10301", "HR", "departments", "view", "View departments."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10302", "HR", "departments", "create", "Create departments."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10303", "HR", "departments", "edit", "Update departments."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10304", "HR", "departments", "delete", "Delete departments."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10401", "HR", "employees", "view", "View employees."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10402", "HR", "employees", "create", "Create employees."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10403", "HR", "employees", "edit", "Update employees."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10404", "HR", "employees", "delete", "Delete employees."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10501", "Banking", "customers", "view", "View customers."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10502", "Banking", "customers", "create", "Create customers."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10503", "Banking", "customers", "edit", "Update customers."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10504", "Banking", "customers", "delete", "Delete customers."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10601", "Platform", "menus", "view", "View menu catalog."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10602", "Platform", "menus", "create", "Create menu items."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10603", "Platform", "menus", "edit", "Update menu items."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10604", "Platform", "menus", "delete", "Delete menu items."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10701", "Shared", "payments", "view", "View payment history and provider readiness."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10702", "Shared", "payments", "create", "Initiate payment checkout flows."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10703", "Shared", "payments", "admin", "Manage M-Pesa payment administration actions."),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9b10801", "ControlPlane", "controlplane", "manage", "Manage Control Plane Tenants and Stamps.")
    ];
    private static Permission Create(string id, string context, string resource, string action, string description)
    {
        var permission = Permission.Create(null, context, resource, action, description, "System");
        permission.Id = Guid.Parse(id);
        permission.TenantId = SeedTenantId;
        permission.CreatedAt = SeedCreatedAt;
        return permission;
    }
}
