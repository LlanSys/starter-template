using System.Net;
using OtpNet;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using System.Net.Http.Json;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;

namespace ST.Tests.Integration.ControlPlane;

public sealed partial class PlatformIamSignInTests
{
    [Fact]
    public async Task SuspendedCustomerIsUnavailableWithoutServerErrorAndOtherCustomerStillWorks()
    {
        using var app = CreateHost();
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenantA);
        await factory.RegisterTenantAsync(tenantB);
        var operatorIdentity = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "Lifecycle", true,
            "control_plane.view_tenants,control_plane.manage_tenant_lifecycle");

        var readerA = await CreateIdentityAsync(app, tenantA, "SuspensionA", false, "users.view");
        var readerB = await CreateIdentityAsync(app, tenantB, "SuspensionB", false, "users.view");
        using var lifecycle = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, operatorIdentity.Name, true));
        string? seed = null;
        using var a = Bearer(app, await SignInWithAuthenticatorAsync(app, tenantA, readerA.Name, false, key => seed = key));
        using var b = Bearer(app, await SignInWithAuthenticatorAsync(app, tenantB, readerB.Name, false));
        await ExpectAsync(a, "/api/v1/iam/admin/users", true);
        await PostAsync<TenantResponse>(lifecycle, $"/api/v1/control-plane/tenants/{tenantA}/suspend", null);
        using var unavailable = await a.GetAsync("/api/v1/iam/admin/users");
        Assert.Contains(unavailable.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized });
        Assert.DoesNotContain("Exception", await unavailable.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        await ExpectAsync(b, "/api/v1/iam/admin/users", true);
        var reactivated = await PostAsync<TenantResponse>(lifecycle, $"/api/v1/control-plane/tenants/{tenantA}/activate", null);
        Assert.NotNull(reactivated.KycApprovedAt);
        Assert.Equal("Active", reactivated.Status);
        using var oldSession = await a.GetAsync("/api/v1/iam/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
        using var anonymous = app.CreateClient(new()
        {
            HandleCookies = false
        });

        anonymous.DefaultRequestHeaders.Add("X-Tenant-Id", tenantA.ToString());
        var challenge = await PostAsync<LoginResponse>(anonymous, "/api/v1/iam/auth/login",
            new
            {
                UserName = readerA.Name,
                Password,
                DeviceFingerprint = "platform-matrix"
            });

        Assert.True(challenge.Requires2FA);
        var verified = await PostAsync<VerifyOtpResponse>(anonymous, "/api/v1/iam/users/totp/verify", new VerifyOtpRequest
        {
            UserId = readerA.UserId,
            Code = new Totp(Base32Encoding.ToBytes(seed!)).ComputeTotp(),
            ChallengeToken = challenge.Token,
            DeviceFingerprint = "platform-matrix"
        });

        using var fresh = Bearer(app, verified.Token);
        await ExpectAsync(fresh, "/api/v1/iam/admin/users", true);
    }

    [Fact]
    public async Task LimitedTenantNavigationExposesOnlyUsableAdministrationPages()
    {
        using var app = CreateHost();
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        var reader = await CreateIdentityAsync(app, tenant, "NavigationReader", false, "users.view");
        using var client = Bearer(app, await SignInWithAuthenticatorAsync(app, tenant, reader.Name, false));
        var menus = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<MenuResponse>>>(
            "/api/v1/iam/admin/menus/navigation/AdminCenter");

        Assert.True(menus!.IsSuccess);
        var urls = Flatten(menus.Data!).Select(item => item.Url).ToArray();
        Assert.Contains("/admin/iam", urls);
        Assert.DoesNotContain("/admin/menus", urls);
        Assert.DoesNotContain("/admin/access-catalog", urls);
        Assert.DoesNotContain("/admin/org-settings", urls);
        await ExpectAsync(client, "/api/v1/iam/admin/users", true);
        await ExpectAsync(client, "/api/v1/iam/admin/roles", false);
    }

    [Fact]
    public async Task RealTenantSessionsKeepCachedSettingsIsolatedAcrossReadsAndMutations()
    {
        using var app = CreateHost();
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenantA);
        await factory.RegisterTenantAsync(tenantB);
        const string permissions = "tenant-settings.view,tenant-settings.create,tenant-settings.update,tenant-settings.delete";
        var platform = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "CacheDecoy", false, permissions);
        var userA = await CreateIdentityAsync(app, tenantA, "CacheA", false, permissions);
        var userB = await CreateIdentityAsync(app, tenantB, "CacheB", false, permissions);
        using var root = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, platform.Name, false));
        using var a = Bearer(app, await SignInWithAuthenticatorAsync(app, tenantA, userA.Name, false));
        using var b = Bearer(app, await SignInWithAuthenticatorAsync(app, tenantB, userB.Name, false));
        const string route = "/api/v1/shared/tenant-settings";
        const string key = "presentation.time-zone";
        async Task SetAsync(HttpClient client, string value, string? revision = null)
        {
            using var response = await client.PutAsJsonAsync($"{route}/catalogue/{key}", new SaveCatalogOrgSettingRequest(value, revision));
            response.EnsureSuccessStatusCode();
        }
        async Task<CatalogOrgSettingResponse> SettingAsync(HttpClient client)
        {
            var response = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<CatalogOrgSettingResponse>>>(route + "/catalogue");
            return Assert.Single(response!.Data!, item => item.Definition.Key == key);
        }
        await SetAsync(root, "Pacific/Auckland", (await SettingAsync(root)).Revision);
        await a.GetAsync(route + "/catalogue");
        await b.GetAsync(route + "/catalogue");
        await SetAsync(a, "Africa/Nairobi");
        await SetAsync(b, "America/New_York");
        var itemA = await SettingAsync(a);
        var itemB = await SettingAsync(b);

        async Task VerifyAsync(HttpClient client, string expected)
        {
            var single = await client.GetFromJsonAsync<AppResponse<OrgSettingResponse>>($"{route}/{key}");
            Assert.True(single!.IsSuccess, single.Message);
            Assert.Equal(expected, single.Data!.Value);
            var all = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<OrgSettingResponse>>>(route);
            Assert.True(all!.IsSuccess, all.Message);
            Assert.Equal(expected, Assert.Single(all.Data!, item => item.Key == key).Value);
            Assert.Equal(expected, (await SettingAsync(client)).Value);
            var presentation = await client.GetFromJsonAsync<AppResponse<PresentationSettingsResponse>>(route + "/presentation");
            Assert.Equal(expected, presentation!.Data!.TimeZone);
        }

        await Task.WhenAll(Enumerable.Range(0, 12).Select(i => i % 2 == 0
            ? VerifyAsync(a, "Africa/Nairobi") : VerifyAsync(b, "America/New_York")));

        await SetAsync(a, "Europe/London", itemA.Revision);
        await VerifyAsync(a, "Europe/London");
        await VerifyAsync(b, "America/New_York");
        using var cross = await a.PutAsJsonAsync(route + "/catalogue/" + key, new SaveCatalogOrgSettingRequest("UTC", itemB.Revision));
        Assert.Equal(HttpStatusCode.Conflict, cross.StatusCode);
        var latestA = await SettingAsync(a);
        using var reset = await a.PostAsJsonAsync(route + "/catalogue/" + key + "/reset", new SaveCatalogOrgSettingRequest("", latestA.Revision));
        reset.EnsureSuccessStatusCode();
        Assert.False((await SettingAsync(a)).IsOverridden);
        using var absent = await a.GetAsync($"{route}/{key}");
        Assert.False(absent.IsSuccessStatusCode);
        await VerifyAsync(b, "America/New_York");
    }
}
