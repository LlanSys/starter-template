using ST.Api.Common.Authorization;
using ST.SharedKernel.Features.ControlPlane;
using ST.Tests.Unit.TestFixtures;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ST.Tests.Unit.Api.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Succeed_When_User_Has_Required_Permission()
    {
        var user = CreatePrincipal(new Claim(PermissionAuthorizationConstants.ClaimType, "users.edit"));
        var requirement = new PermissionRequirement("users.edit");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler(new TestModulePolicy()).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_For_System_Administrator()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.Role, PermissionAuthorizationConstants.SystemAdministratorRole));
        var requirement = new PermissionRequirement("roles.delete");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler(new TestModulePolicy()).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Succeed_When_Permission_Is_Missing()
    {
        var user = CreatePrincipal(new Claim(PermissionAuthorizationConstants.ClaimType, "users.view"));
        var requirement = new PermissionRequirement("users.edit");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler(new TestModulePolicy()).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task PlatformAccessRequiresStaffAndExplicitPermission(bool staff, bool permission, bool expected)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "System Administrator") };
        if (staff) claims.Add(new Claim("platform_staff", "true"));
        if (permission) claims.Add(new Claim("permission", "control_plane.manage"));
        var requirement = new PermissionRequirement("control_plane.manage");
        var context = new AuthorizationHandlerContext([requirement], CreatePrincipal(claims.ToArray()), null);
        await new PermissionAuthorizationHandler(new TestModulePolicy()).HandleAsync(context);
        Assert.Equal(expected, context.HasSucceeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlatformManagementAloneCannotApproveKyc(bool approvalGranted)
    {
        var claims = new List<Claim>
        {
            new("platform_staff", "true"),
            new("permission", PlatformAccess.Permission)
        };
        if (approvalGranted)
        {
            claims.Add(new Claim("permission", PlatformAccess.ApproveKycPermission));
        }

        var requirement = new PermissionRequirement(PlatformAccess.ApproveKycPermission);
        var context = new AuthorizationHandlerContext([requirement], CreatePrincipal(claims.ToArray()), null);
        await new PermissionAuthorizationHandler(new TestModulePolicy()).HandleAsync(context);

        Assert.Equal(approvalGranted, context.HasSucceeded);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
