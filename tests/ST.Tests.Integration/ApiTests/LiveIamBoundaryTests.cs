using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST.Persistence.Features.ControlPlane.DataContext;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ApiTests;

[Collection("Starter API")]
public sealed class LiveIamBoundaryTests(StarterTemplateWebApplicationFactory factory)
{
    private const string Admin = "/api/v1/iam/admin";

    [Fact]
    public async Task DelegatedRoleEditorCannotCreateOrRenameIntoReservedAuthority()
    {
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        using var editor = Client(tenant, "roles.create,roles.edit");
        var role = await CreateRole(editor, "Operator");

        using var maliciousRename = await editor.PutAsJsonAsync(
            $"{Admin}/roles/{role.Id}", new UpdateRoleRequest("  system administrator  "));
        var denied = await maliciousRename.Content.ReadFromJsonAsync<AppResponse<AdminRoleListResponse>>();
        Assert.False(denied!.IsSuccess);

        using var validRename = await editor.PutAsJsonAsync(
            $"{Admin}/roles/{role.Id}", new UpdateRoleRequest("Operator Revised"));
        Assert.True((await validRename.Content.ReadFromJsonAsync<AppResponse<AdminRoleListResponse>>())!.IsSuccess);
    }

    [Fact]
    public async Task CreatingOrEditingUsersCannotDelegateHigherPermissionsThroughRoles()
    {
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        using var administrator = Client(tenant, "roles.create,roles.manage_permissions,users.view");
        var role = await CreateRole(administrator, "Readers");
        using var setRolePermissions = await administrator.PutAsJsonAsync(
            $"{Admin}/roles/{role.Id}/permissions", new { PermissionKeys = new[] { "users.view" } });
        Assert.True((await setRolePermissions.Content.ReadFromJsonAsync<AppResponse<RolePermissionsResponse>>())!.IsSuccess);

        using var creator = Client(tenant, "users.create,users.manage_roles,users.manage_permissions");
        using var deniedCreate = await creator.PostAsJsonAsync($"{Admin}/users",
            new CreateAppUserRequest("denied", "denied@example.test", "StrongPassword42!", "Denied", "User", null, ["Readers"]));
        Assert.False((await deniedCreate.Content.ReadFromJsonAsync<AppResponse<AppUserResponse>>())!.IsSuccess);

        using var allowedCreate = await creator.PostAsJsonAsync($"{Admin}/users",
            new CreateAppUserRequest("allowed", "allowed@example.test", "StrongPassword42!", "Allowed", "User", null, []));
        var account = await allowedCreate.Content.ReadFromJsonAsync<AppResponse<AppUserResponse>>();
        Assert.True(account!.IsSuccess);

        using var deniedAssignment = await creator.PutAsJsonAsync(
            $"{Admin}/users/{account.Data!.Id}/roles", new { Roles = new[] { "Readers" } });
        Assert.False((await deniedAssignment.Content.ReadFromJsonAsync<AppResponse<UserRolesResponse>>())!.IsSuccess);

        using var deniedDirectGrant = await creator.PutAsJsonAsync(
            $"{Admin}/users/{account.Data.Id}/permissions", new { PermissionKeys = new[] { "users.view" } });
        Assert.False((await deniedDirectGrant.Content.ReadFromJsonAsync<AppResponse<UserPermissionsResponse>>())!.IsSuccess);
    }

    [Fact]
    public async Task ExistingBearerLosesDirectAndRoleGrantsWithoutRefresh()
    {
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        services.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenant;
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var roles = services.GetRequiredService<RoleManager<AppRole>>();
        var user = AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, tenant, "live-user", "live-user@example.test", "test");
        user.IsActive = true;
        user.EmailConfirmed = true;
        Assert.True((await users.CreateAsync(user)).Succeeded);
        var role = new AppRole { Name = "Readers", TenantId = tenant };
        Assert.True((await roles.CreateAsync(role)).Succeeded);
        var permission = new Claim("permission", "users.view");
        Assert.True((await roles.AddClaimAsync(role, permission)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role.Name)).Succeeded);
        Assert.True((await users.AddClaimAsync(user, permission)).Succeeded);

        var session = await services.GetRequiredService<ISessionService>()
            .CreateSessionAsync(user.Id, Guid.CreateVersion7(), "127.0.0.1", "integration");
        Assert.True(session.IsSuccess);
        var claims = await services.GetRequiredService<IClaimsService>().GetUserClaimsAsync(user, session.Data);
        var jwt = services.GetRequiredService<IJwtService>();
        var token = await jwt.CreateTokenAsync(claims);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Admin}/users")).StatusCode);
        Assert.True((await users.RemoveClaimAsync(user, permission)).Succeeded);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Admin}/users")).StatusCode);
        Assert.True((await users.RemoveFromRoleAsync(user, role.Name)).Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Admin}/users")).StatusCode);
        Assert.True((await users.AddClaimAsync(user, permission)).Succeeded);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Admin}/users")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            await jwt.CreateTokenAsync(claims.Where(claim => claim.Type != "session_id").ToList()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Admin}/users")).StatusCode);

        var control = services.GetRequiredService<ControlPlaneDBContext>();
        var workspace = await control.Tenants.SingleAsync(item => item.Id == tenant);
        workspace.Suspend();
        await control.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Admin}/users")).StatusCode);

        workspace.Reactivate(TimeProvider.System.GetUtcNow());
        await control.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Admin}/users")).StatusCode);

        var freshSession = await services.GetRequiredService<ISessionService>()
            .CreateSessionAsync(user.Id, Guid.CreateVersion7(), "127.0.0.1", "integration");
        Assert.True(freshSession.IsSuccess);
        var freshClaims = await services.GetRequiredService<IClaimsService>().GetUserClaimsAsync(user, freshSession.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await jwt.CreateTokenAsync(freshClaims));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Admin}/users")).StatusCode);
    }

    private static async Task<AdminRoleListResponse> CreateRole(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync($"{Admin}/roles", new CreateRoleRequest(name));
        var body = await response.Content.ReadFromJsonAsync<AppResponse<AdminRoleListResponse>>();
        Assert.True(body!.IsSuccess, body.Message);
        return body.Data!;
    }

    private HttpClient Client(Guid tenant, string permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.CreateVersion7().ToString());
        client.DefaultRequestHeaders.Add("X-Test-TenantId", tenant.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        return client;
    }

}
