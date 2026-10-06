using ST.Api.Common.Authorization;
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

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_For_System_Administrator()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.Role, PermissionAuthorizationConstants.SystemAdministratorRole));
        var requirement = new PermissionRequirement("roles.delete");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Succeed_When_Permission_Is_Missing()
    {
        var user = CreatePrincipal(new Claim(PermissionAuthorizationConstants.ClaimType, "users.view"));
        var requirement = new PermissionRequirement("users.edit");
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
