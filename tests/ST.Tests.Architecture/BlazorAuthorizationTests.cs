using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using ST.UI.Blazor.Features.Shared.OrgSettings.Pages;

namespace ST.Tests.Architecture;

public sealed class BlazorAuthorizationTests
{
    [Fact]
    public void Organization_Settings_Page_Retains_Its_Route_And_Policy()
    {
        var page = typeof(AdminOrgSettings);
        var route = Assert.Single(page.GetCustomAttributes<RouteAttribute>());
        var authorization = Assert.Single(page.GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal("/admin/org-settings", route.Template);
        Assert.Equal("Page:" + route.Template, authorization.Policy);
        Assert.Null(page.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
