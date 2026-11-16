using System.Security.Claims;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.IAM.Users.Authorization;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Tests.Unit;

public sealed class CurrentUserPermissionsTests
{
    [Fact]
    public void ExplicitPlatformIdentityGrantsSurviveTenantModuleUnavailability()
    {
        var user = User(true);
        var keys = CurrentUserPermissions.EffectiveKeys(user);
        Assert.Contains(PlatformPermissions.ViewPlatformStaff, keys);
        Assert.True(CurrentUserPermissions.HasPermission(user, PlatformPermissions.ViewPlatformStaff));
        var claims = keys.Select(key => new Claim("permission", key)).Append(new Claim(PlatformAccess.StaffClaim, "true"))
            .Append(new Claim("amr", "mfa"));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "InteractiveSession"));
        Assert.True(PlatformAccess.Can(principal, PlatformPermissions.ViewPlatformStaff));
        Assert.True(PlatformAccess.HasMfa(principal));
    }

    [Fact]
    public void OrdinaryUnavailableAndRetiredPlatformKeysNeverBecomeInteractiveAuthority()
    {
        var user = User(true);
        var keys = CurrentUserPermissions.EffectiveKeys(user);
        Assert.DoesNotContain("menus.view", keys);
        Assert.DoesNotContain("control_plane.manage", keys);
        Assert.False(CurrentUserPermissions.HasPermission(user, "menus.view"));
        Assert.False(CurrentUserPermissions.HasPermission(user, "control_plane.manage"));
        Assert.Contains("users.view", keys);
    }

    [Fact]
    public void PermissionTextWithoutPlatformIdentityCannotGrantPlatformAccess()
    {
        var user = User(false);
        Assert.DoesNotContain(PlatformPermissions.ViewPlatformStaff, CurrentUserPermissions.EffectiveKeys(user));
        Assert.False(CurrentUserPermissions.HasPermission(user, PlatformPermissions.ViewPlatformStaff));
    }

    private static CurrentUserResponse User(bool staff)
    {
        return new CurrentUserResponse("fixture", Guid.Empty, "fixture", "fixture@example.test", "QA", "Fixture", "",
            true, true, true, null, [], [PlatformPermissions.ViewPlatformStaff, "control_plane.manage", "menus.view", "users.view"])
        {
            IsPlatformStaff = staff,
            HasMfaProof = true,
            UnavailablePermissions = [PlatformPermissions.ViewPlatformStaff, "control_plane.manage", "menus.view"]
        };
    }
}
