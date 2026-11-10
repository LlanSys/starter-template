using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.NeutralSlice;

[Collection("Starter API")]
public sealed class NeutralVerticalSliceTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task TestOnlySliceCrossesApiHandlerAndRepositoryWithoutEditingIam()
    {
        var tenantA=Guid.CreateVersion7();var tenantB=Guid.CreateVersion7();await factory.RegisterTenantAsync(tenantA);await factory.RegisterTenantAsync(tenantB);
        using var owner = Client(tenantA, "notes.create,notes.view");
        using var foreign = Client(tenantB, "notes.view");
        using var missingPermission = Client(Guid.CreateVersion7(), "");
        var response = await owner.PostAsJsonAsync("/api/v1/certification/notes", "Neutral extension");
        response.EnsureSuccessStatusCode();
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);
        using var ownRead = await owner.GetAsync($"/api/v1/certification/notes/{id}");
        Assert.Equal(HttpStatusCode.OK, ownRead.StatusCode);
        Assert.Contains("Neutral extension", await ownRead.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using var foreignRead = await foreign.GetAsync($"/api/v1/certification/notes/{id}");
        Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);
        using var denied = await missingPermission.GetAsync($"/api/v1/certification/notes/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var anonymous = factory.CreateClient();
        using var unauthenticated = await anonymous.GetAsync($"/api/v1/certification/notes/{id}");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
    }
    private HttpClient Client(Guid tenant, string permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-TenantId", tenant.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        return client;
    }
}
