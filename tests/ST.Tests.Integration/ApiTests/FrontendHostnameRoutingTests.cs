using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using ST.Domain.Features.IAM.Users.Entities;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Tenants;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.ApiTests;

[Collection("Starter API")]
public sealed class FrontendHostnameRoutingTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task RegisteredCompanyAddressBindsAnonymousDiscoveryAndRejectsOtherSelectorsAndSessions()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(a);
        await factory.RegisterTenantAsync(b);
        var origin = $"http://{a:N}.localhost:8181";
        using var staff = await StaffClientAsync();
        await SaveAddressAsync(staff, a, origin);
        using var browser = factory.CreateClient();
        browser.DefaultRequestHeaders.Add(FrontendOrigin.HeaderName, origin);
        var info = await browser.GetFromJsonAsync<AppResponse<WorkspaceInfoResponse>>("/api/v1/workspaces/resolve");
        Assert.True(info!.IsSuccess);
        Assert.Equal(a, info.Data!.TenantId);
        Assert.True(info.Data.IsFrontendBound);
        Assert.Equal(origin, info.Data.ClientOrigin);
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync($"/api/v1/workspaces/resolve?tenantId={b}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await browser.PostAsJsonAsync($"/api/v1/onboarding/kyc/{b}/inspect", new
        {
            token = "synthetic-invalid-token"
        })).StatusCode);

        browser.DefaultRequestHeaders.Add("X-Tenant-Id", b.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.GetAsync("/api/v1/workspaces/resolve")).StatusCode);
        browser.DefaultRequestHeaders.Remove("X-Tenant-Id");
        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        browser.DefaultRequestHeaders.Add("X-Test-TenantId", b.ToString());
        browser.DefaultRequestHeaders.Add("X-Test-Permissions", "users.view");
        Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/api/v1/iam/admin/users")).StatusCode);
        browser.DefaultRequestHeaders.Remove("X-Test-TenantId");
        browser.DefaultRequestHeaders.Add("X-Test-TenantId", a.ToString());
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/v1/iam/admin/users")).StatusCode);
    }

    [Fact]
    public async Task AddressChangesRequireOperationPermissionUniqueOriginAndCurrentRevision()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(a);
        await factory.RegisterTenantAsync(b);
        using var staff = await StaffClientAsync();
        var revision = await ReadAddressAsync(staff, a);
        var origin = $"https://{a:N}.example.test";
        await SaveAddressAsync(staff, a, origin);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PutAsJsonAsync(Path(a), new SaveTenantFrontendAddressRequest(null, revision.Revision))).StatusCode);
        var other = await ReadAddressAsync(staff, b);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PutAsJsonAsync(Path(b), new SaveTenantFrontendAddressRequest(origin, other.Revision))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await staff.PutAsJsonAsync(Path(b), new SaveTenantFrontendAddressRequest("http://localhost:8181", other.Revision))).StatusCode);
        staff.DefaultRequestHeaders.Remove("X-Test-Permissions");
        staff.DefaultRequestHeaders.Add("X-Test-Permissions", PlatformPermissions.ViewTenants);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync(Path(b), new SaveTenantFrontendAddressRequest(null, other.Revision))).StatusCode);
        // Origin information is public routing metadata; it never grants administrative authority.
        staff.DefaultRequestHeaders.Remove("X-Test-PlatformStaff");
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync(Path(a))).StatusCode);
    }

    [Fact]
    public async Task UnregisteredMalformedAndSuspendedCompanyAddressesFailClosedWhileSharedPortalRemainsAvailable()
    {
        var a = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(a);
        var origin = $"https://{a:N}.example.test";
        using var staff = await StaffClientAsync();
        await SaveAddressAsync(staff, a, origin);
        using var browser = factory.CreateClient();

        foreach (var (address, expected) in new[]
        {
            ("https://unknown.example.test", HttpStatusCode.NotFound),
            ("https://app.example.test/path", HttpStatusCode.BadRequest),
            ("http://localhost:8181", HttpStatusCode.OK)
        })
        {
            browser.DefaultRequestHeaders.Remove(FrontendOrigin.HeaderName);
            browser.DefaultRequestHeaders.Add(FrontendOrigin.HeaderName, address);
            Assert.Equal(expected, (await browser.GetAsync("/api/v1/workspaces/resolve")).StatusCode);
        }
        staff.DefaultRequestHeaders.Remove("X-Test-Permissions");
        staff.DefaultRequestHeaders.Add("X-Test-Permissions", PlatformPermissions.ManageTenantLifecycle);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsync($"/api/v1/control-plane/tenants/{a}/suspend", null)).StatusCode);
        browser.DefaultRequestHeaders.Remove(FrontendOrigin.HeaderName);
        browser.DefaultRequestHeaders.Add(FrontendOrigin.HeaderName, origin);
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync("/api/v1/workspaces/resolve")).StatusCode);
    }

    [Fact]
    public async Task SsoReturnUrlMustBelongToTheRequestedActiveCompany()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(a);
        await factory.RegisterTenantAsync(b);
        var origin = $"http://{a:N}.localhost:8181";
        using var staff = await StaffClientAsync();
        await SaveAddressAsync(staff, a, origin);
        using var enabled = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<AuthenticationOptions>(options =>
            {
                var scheme = options.Schemes.FirstOrDefault(item => item.Name == "Google");

                if (scheme is null)
                {
                    options.AddScheme("Google", definition => definition.HandlerType = typeof(TestAuthHandler));
                }
                else
                {
                    scheme.HandlerType = typeof(TestAuthHandler);
                }
            })));

        using var client = enabled.CreateClient(new()
        {
            AllowAutoRedirect = false
        });

        string Login(Guid id, string address) => $"/api/v1/iam/auth/sso/login?tenantId={id}&provider=Google&returnUrl={Uri.EscapeDataString(address)}";
        // The test scheme challenges with 401 after validation; a rejected return target responds 400 before challenge.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Login(a, origin + "/iam/sso-callback"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Login(b, origin + "/iam/sso-callback"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Login(a, "https://unregistered.example.test/iam/sso-callback"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Login(a, origin + "/iam/sso-callback?next=evil"))).StatusCode);
    }

    [Fact]
    public async Task PublicDiscoveryHasItsOwnBoundedBudgetWithoutConsumingCredentialAttempts()
    {
        using var isolated = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:WorkspaceDiscoveryPolicy:PermitLimit"] = "2",
                ["RateLimiting:WorkspaceDiscoveryPolicy:WindowSeconds"] = "60",
                ["RateLimiting:WorkspaceDiscoveryPolicy:QueueLimit"] = "0",
                ["RateLimiting:LoginPolicy:PermitLimit"] = "1",
                ["RateLimiting:LoginPolicy:WindowSeconds"] = "60",
                ["RateLimiting:LoginPolicy:QueueLimit"] = "0"
            })));

        using var client = isolated.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/workspaces/resolve")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/workspaces/resolve")).StatusCode);
        var started = TimeProvider.System.GetTimestamp();
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/v1/workspaces/resolve")).StatusCode);
        Assert.True(TimeProvider.System.GetElapsedTime(started) < TimeSpan.FromSeconds(5));
        var credentials = new
        {
            userName = "nonexistent-discovery-test",
            password = "Synthetic!Credential2",
            rememberMe = false
        };

        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/iam/auth/login", credentials)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/iam/auth/login", credentials)).StatusCode);
    }

    private async Task<HttpClient> StaffClientAsync()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var identity = $"host-operator-{Guid.CreateVersion7():N}";
        var actor = AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, Tenant.PlatformTenantId, identity, identity + "@example.test", "test-fixture");
        actor.IsActive = true;
        actor.EmailConfirmed = true;
        actor.TwoFactorEnabled = true;
        Assert.True((await users.CreateAsync(actor)).Succeeded);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-TenantId", Tenant.PlatformTenantId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-PlatformStaff", "true");
        client.DefaultRequestHeaders.Add("X-Test-UserId", actor.Id);
        client.DefaultRequestHeaders.Add("X-Test-Permissions", PlatformPermissions.ViewTenants + "," + PlatformPermissions.EditTenants);
        return client;
    }

    private static string Path(Guid id)
    {
        return $"/api/v1/control-plane/tenants/{id}/frontend-address";
    }

    private static async Task<TenantFrontendAddressResponse> ReadAddressAsync(HttpClient client, Guid id)
    {
        var result = await client.GetFromJsonAsync<AppResponse<TenantFrontendAddressResponse>>(Path(id));
        Assert.True(result!.IsSuccess);
        return result.Data!;
    }

    private static async Task SaveAddressAsync(HttpClient client, Guid id, string origin)
    {
        var state = await ReadAddressAsync(client, id);
        using var response = await client.PutAsJsonAsync(Path(id), new SaveTenantFrontendAddressRequest(origin, state.Revision));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
