using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.ApiTests;

[Collection("Starter API")]
public sealed class OrganizationSettingsCatalogueTests(StarterTemplateWebApplicationFactory factory)
{
    private const string Root = "/api/v1/shared/tenant-settings";
    private const string Manage = "tenant-settings.view,tenant-settings.update,tenant-settings.delete";

    [Fact]
    public async Task MembersReceiveOnlyPresentationWhileAdministrationRequiresPermission()
    {
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        using var member = Client(tenant, "");
        var defaults = await member.GetFromJsonAsync<AppResponse<PresentationSettingsResponse>>(Root + "/presentation");
        Assert.Equal(PresentationSettingsResponse.Default, defaults!.Data);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync(Root + "/catalogue")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync(Root + "/history")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Save(member, OrganizationSettingCatalog.PageSize, "50")).StatusCode);
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root + "/presentation")).StatusCode);
    }

    [Fact]
    public async Task SaveResetAndHistoryRemainTenantScopedAndResetRemovesTheOverride()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenantA);
        await factory.RegisterTenantAsync(tenantB);
        using var a = Client(tenantA, Manage);
        using var b = Client(tenantB, Manage);
        (await Save(a, OrganizationSettingCatalog.TimeZone, "Africa/Nairobi")).EnsureSuccessStatusCode();
        (await Save(b, OrganizationSettingCatalog.TimeZone, "America/New_York")).EnsureSuccessStatusCode();
        var aSetting = (await Catalogue(a)).Single(item => item.Definition.Key == OrganizationSettingCatalog.TimeZone);
        var bSetting = (await Catalogue(b)).Single(item => item.Definition.Key == OrganizationSettingCatalog.TimeZone);
        Assert.Equal("Africa/Nairobi", aSetting.Value);
        Assert.Equal("America/New_York", bSetting.Value);
        Assert.Equal(HttpStatusCode.Conflict, (await b.PostAsJsonAsync(Root + "/catalogue/" + OrganizationSettingCatalog.TimeZone + "/reset", new SaveCatalogOrgSettingRequest("", aSetting.Revision))).StatusCode);
        (await a.PostAsJsonAsync(Root + "/catalogue/" + OrganizationSettingCatalog.TimeZone + "/reset", new SaveCatalogOrgSettingRequest("", aSetting.Revision))).EnsureSuccessStatusCode();
        var reset = (await Catalogue(a)).Single(item => item.Definition.Key == OrganizationSettingCatalog.TimeZone);
        Assert.Equal("UTC", reset.Value);
        Assert.False(reset.IsOverridden);
        Assert.Null(reset.Revision);
        (await Save(a, OrganizationSettingCatalog.TimeZone, "Europe/London")).EnsureSuccessStatusCode();
        Assert.Equal("America/New_York", (await Catalogue(b)).Single(item => item.Definition.Key == OrganizationSettingCatalog.TimeZone).Value);
        var history = await a.GetFromJsonAsync<AppResponse<IReadOnlyList<OrgSettingChangeResponse>>>(Root + "/history");
        Assert.Equal(3, history!.Data!.Count);
        Assert.Single(history.Data.Where(change => change.Reset));
        Assert.All(history.Data, change => Assert.Equal("settings-test-actor", change.ActorId));
        var bhistory = await b.GetFromJsonAsync<AppResponse<IReadOnlyList<OrgSettingChangeResponse>>>(Root + "/history");
        Assert.Single(bhistory!.Data!);
    }

    [Fact]
    public async Task InvalidValuesAndLegacyWritesCannotBypassTheCatalogue()
    {
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        using var a = Client(tenant, Manage + ",tenant-settings.create");
        Assert.Equal(HttpStatusCode.BadRequest, (await Save(a, OrganizationSettingCatalog.PageSize, "100000")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Save(a, "JwtSettings:SecretKey", "value")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await a.PostAsJsonAsync(Root, new CreateOrgSettingRequest("arbitrary", "value", null))).StatusCode);
        Assert.All(await Catalogue(a), setting => Assert.False(setting.IsOverridden));
    }

    [Fact]
    public async Task ConcurrentInitialSavesProduceOneWinnerAndARefreshConflict()
    {
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        using var a = Client(tenant, Manage);
        using var b = Client(tenant, Manage);
        var responses = await Task.WhenAll(Save(a, OrganizationSettingCatalog.PageSize, "50"), Save(b, OrganizationSettingCatalog.PageSize, "100"));
        Assert.Single(responses.Where(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Single(responses.Where(response => response.StatusCode == HttpStatusCode.Conflict));
        var current = (await Catalogue(a)).Single(setting => setting.Definition.Key == OrganizationSettingCatalog.PageSize);
        Assert.True(current.IsOverridden);
        Assert.Equal(HttpStatusCode.Conflict, (await Save(a, OrganizationSettingCatalog.PageSize, "25")).StatusCode);
        (await a.PutAsJsonAsync(Root + "/catalogue/" + OrganizationSettingCatalog.PageSize, new SaveCatalogOrgSettingRequest("10", current.Revision))).EnsureSuccessStatusCode();
        var presentation = await a.GetFromJsonAsync<AppResponse<PresentationSettingsResponse>>(Root + "/presentation");
        Assert.Equal(10, presentation!.Data!.PageSize);
    }

    private static Task<HttpResponseMessage> Save(HttpClient client, string key, string value)
    {
        return client.PutAsJsonAsync(Root + "/catalogue/" + Uri.EscapeDataString(key), new SaveCatalogOrgSettingRequest(value, null));
    }

    private static async Task<IReadOnlyList<CatalogOrgSettingResponse>> Catalogue(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<CatalogOrgSettingResponse>>>(Root + "/catalogue");
        Assert.True(response!.IsSuccess);
        return response.Data!;
    }

    private HttpClient Client(Guid tenant, string permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-TenantId", tenant.ToString());
        client.DefaultRequestHeaders.Add("X-Test-UserId", "settings-test-actor");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        return client;
    }
}
