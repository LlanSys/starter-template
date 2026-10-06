using System.Reflection;
using ST.Api.Common.Authorization;
using ST.Api.Features.IAM.Users.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace ST.Tests.Unit.Infrastructure.IAM;

public sealed class AccountAccessAuthorizationTests
{
    [Theory]
    [InlineData(nameof(IamAdminController.ActivateUser), "users.edit")]
    [InlineData(nameof(IamAdminController.DeactivateUser), "users.deactivate")]
    public void AccessEndpointsRequireAuthenticationAndSpecificPermission(string methodName, string permission)
    {
        var controller = typeof(IamAdminController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        var method = controller.GetMethod(methodName)!;
        Assert.Equal(PermissionAuthorizationConstants.BuildPolicyName(permission), method.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}