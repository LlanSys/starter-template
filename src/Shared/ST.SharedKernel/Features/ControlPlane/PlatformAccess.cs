using System.Security.Claims;

namespace ST.SharedKernel.Features.ControlPlane;

public static class PlatformAccess
{
    public const string StaffClaim = "platform_staff";
    public const string ApproveKycPermission = PlatformPermissions.ApproveKyc;

    public static bool HasMfa(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.Claims.Any(claim => (claim.Type == "amr"
            || claim.Type == "http://schemas.microsoft.com/claims/authnmethodsreferences") && claim.Value == "mfa");
    }

    public static bool IsStaff(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.Identity?.IsAuthenticated == true && user.HasClaim(StaffClaim, "true");
    }

    public static bool Can(ClaimsPrincipal user, string permission)
    {
        return IsStaff(user) && PlatformPermissions.IsDefined(permission)
            && user.FindAll("permission").Any(claim => string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }

    public static bool CanOpenControlPanel(ClaimsPrincipal user)
    {
        return IsStaff(user) && PlatformPermissions.OpensControlPanel(
            user.FindAll("permission").Select(claim => claim.Value));
    }
}
