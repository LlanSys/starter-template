using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ApiTests;

[Collection("Starter API")]
public sealed class TenantIsolationApiTests(StarterTemplateWebApplicationFactory factory)

{
    [Fact]
    public async Task IndependentAccountCannotBeReadOrActivatedByAnotherTenant()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenantA);await factory.RegisterTenantAsync(tenantB);
        using var clientA = CreateClient(tenantA, "users.create,users.view,users.edit");
        using var clientB = CreateClient(tenantB, "users.view,users.edit");
        var userName = $"neutral-{Guid.CreateVersion7():N}";
        var created = await clientA.PostAsJsonAsync("/api/v1/iam/admin/users",
            new CreateAppUserRequest(userName, $"{userName}@example.test", "StrongPassword42!", "Test", "Account", null, []));
        created.EnsureSuccessStatusCode();
        var account = await created.Content.ReadFromJsonAsync<AppResponse<AppUserResponse>>();
        Assert.True(account!.IsSuccess);
        var id = account.Data!.Id;
        var ownSearch = await clientA.GetFromJsonAsync<AppResponse<PagedResponse<AdminUserListResponse, string>>>(
            $"/api/v1/iam/admin/users?GlobalSearch={userName}");
        Assert.Single(ownSearch!.Data!.Items);
        var foreignSearch = await clientB.GetFromJsonAsync<AppResponse<PagedResponse<AdminUserListResponse, string>>>(
            $"/api/v1/iam/admin/users?GlobalSearch={userName}");
        Assert.Empty(foreignSearch!.Data!.Items);
        using var forbiddenActivation = await clientB.PatchAsync($"/api/v1/iam/admin/users/{id}/activate", null);
        Assert.Equal(HttpStatusCode.NotFound, forbiddenActivation.StatusCode);
        using var permittedActivation = await clientA.PatchAsync($"/api/v1/iam/admin/users/{id}/activate", null);
        permittedActivation.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AccountAdministrationRequiresAuthentication()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/iam/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccountAndApprovalEndpointsRejectMissingPermission()
    {
        using var client = CreateClient(Guid.CreateVersion7(), string.Empty);
        using var accountResponse = await client.GetAsync("/api/v1/iam/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, accountResponse.StatusCode);
        using var approvalResponse = await client.PostAsync($"/api/v1/control-plane/tenants/{Guid.CreateVersion7()}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, approvalResponse.StatusCode);
    }

    private HttpClient CreateClient(Guid tenant, string permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.CreateVersion7().ToString());
        client.DefaultRequestHeaders.Add("X-Test-TenantId", tenant.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        return client;
    }
}
