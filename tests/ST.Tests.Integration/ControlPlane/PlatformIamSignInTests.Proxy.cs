using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Support;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Tests.Integration.ControlPlane;

public sealed partial class PlatformIamSignInTests
{
    [Fact]
    public async Task InternalSupportProxyCannotBecomeAnOrdinaryManagedPersonOrPasswordLogin()
    {
        using var app = CreateHost();
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        var vendor = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "ProxyVendor", true,
            "control_plane.view_tenants,control_plane.view_support,control_plane.manage_support");

        var admin = await CreateIdentityAsync(app, tenant, "ProxyCustomerAdmin", false,
            "users.view,users.edit,users.deactivate,users.manage_roles,users.manage_permissions,support.approve");

        using var platform = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, vendor.Name, true));
        using var customer = Bearer(app, await SignInWithAuthenticatorAsync(app, tenant, admin.Name, false));
        var request = new SupportRequest(tenant, "Synthetic support proxy boundary test", ["users.view"], true, 10);
        var grant = (await PostAsync<SupportResult>(platform, "/api/v1/control-plane/support", request)).Grant;
        await PostAsync<SupportResult>(customer, $"/api/v1/iam/support/{grant.Id}/approve", request);
        var session = (await PostAsync<SupportResult>(platform, $"/api/v1/control-plane/support/{grant.Id}/enter", null)).Session!;
        using var support = Bearer(app, session.Token);
        await ExpectAsync(support, "/api/v1/iam/admin/users", true);
        var people = await customer.GetFromJsonAsync<AppResponse<PagedResponse<AdminUserListResponse, string>>>("/api/v1/iam/admin/users");
        Assert.DoesNotContain(people!.Data!.Items, item => item.Id == session.UserId);
        using var edit = await customer.PutAsJsonAsync($"/api/v1/iam/admin/users/{session.UserId}",
            new UpdateAdminUserRequest("proxy-must-not-be-renamed", "proxy-must-not-be-reassigned@example.test", "Invalid", "Mutation", null, []));

        Assert.Contains(edit.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity });
        using var roles = await customer.PutAsJsonAsync($"/api/v1/iam/admin/users/{session.UserId}/roles", new UpdateUserRolesRequest([]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, roles.StatusCode);
        using var permissions = await customer.PutAsJsonAsync($"/api/v1/iam/admin/users/{session.UserId}/permissions", new UpdateUserPermissionsRequest([]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, permissions.StatusCode);
        using var invite = await customer.PostAsync($"/api/v1/iam/admin/users/{session.UserId}/invite", null);
        Assert.False(invite.IsSuccessStatusCode);
        using var disable = await customer.PatchAsJsonAsync($"/api/v1/iam/admin/users/{session.UserId}/deactivate", new DeactivateUserRequest("Must use support revocation"));
        Assert.False(disable.IsSuccessStatusCode);
        string proxyName;

        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenant;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var proxy = (await users.FindByIdAsync(session.UserId))!;
            proxyName = proxy.UserName!;
            // Simulate an old corrupted fixture: normal entry must reject proxy kind even with a valid hash.
            Assert.True((await users.AddPasswordAsync(proxy, Password)).Succeeded);
        }
        using var anonymous = app.CreateClient(new()
        {
            HandleCookies = false
        });

        anonymous.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.ToString());
        using var login = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/login", new
        {
            UserName = proxyName,
            Password
        });

        Assert.False(login.IsSuccessStatusCode);
        await PostAsync<SupportResult>(customer, $"/api/v1/iam/support/{grant.Id}/revoke", null);
        using var denied = await support.GetAsync("/api/v1/iam/admin/users");
        Assert.Contains(denied.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }
}
