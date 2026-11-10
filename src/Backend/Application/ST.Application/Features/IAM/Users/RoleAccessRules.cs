namespace ST.Application.Features.IAM.Users;

public static class RoleAccessRules
{
    public const string BootstrapRole = "System Administrator";
    public const string TenantAdministratorRole = "Tenant Administrator";

    public static bool IsProtectedRole(string? name)
    {
        return string.Equals(name?.Trim(), BootstrapRole, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name?.Trim(), TenantAdministratorRole, StringComparison.OrdinalIgnoreCase);
    }
}
