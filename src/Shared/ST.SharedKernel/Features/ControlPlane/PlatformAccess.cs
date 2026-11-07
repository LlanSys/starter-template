using System.Security.Claims;
namespace ST.SharedKernel.Features.ControlPlane;

public static class PlatformAccess
{
    public const string StaffClaim = "platform_staff";
    public const string Permission = "control_plane.manage";
    public static bool HasMfa(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.Claims.Any(x => (x.Type == "amr" || x.Type == "http://schemas.microsoft.com/claims/authnmethodsreferences") && x.Value == "mfa");
    }
    public static bool IsStaff(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.Identity?.IsAuthenticated == true && user.HasClaim(StaffClaim, "true") && user.HasClaim("permission", Permission);
    }
}
