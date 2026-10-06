namespace ST.Api.Common.Authorization;

internal static class PermissionAuthorizationConstants
{
    internal const string ClaimType = "permission";
    internal const string PolicyPrefix = "Permission:";
    internal const string SystemAdministratorRole = "System Administrator";

    internal static string BuildPolicyName(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        return $"{PolicyPrefix}{permission.Trim()}";
    }
}
