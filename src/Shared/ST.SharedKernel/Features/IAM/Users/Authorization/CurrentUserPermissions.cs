using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.SharedKernel.Features.IAM.Users.Authorization;

/// <summary>Projects a validated API user snapshot into interactive UI permissions; it does not issue tokens.</summary>
public static class CurrentUserPermissions
{
    public static IReadOnlyList<string> EffectiveKeys(CurrentUserResponse user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.IsAuthenticated == false)
        {
            return [];
        }
        var unavailable = user.UnavailablePermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (user.Permissions ?? []).Where(key => IsAvailable(user, key, unavailable))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static bool HasPermission(CurrentUserResponse user, string permission)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        return user.IsAuthenticated != false
            && user.Permissions?.Contains(permission, StringComparer.OrdinalIgnoreCase) == true
            && IsAvailable(user, permission, user.UnavailablePermissions);
    }

    private static bool IsAvailable(CurrentUserResponse user, string permission, IEnumerable<string> unavailable)
    {
        // Platform grants are explicit identity authority, independent of customer module entitlements.
        return permission.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase)
            ? user.IsPlatformStaff && PlatformPermissions.IsDefined(permission)
            : !unavailable.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}
