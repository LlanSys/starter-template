using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.ControlPlane;
[Collection("Starter API")]
public sealed class ModulePolicyMatrixTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task DefaultsOverridesFlagsTemplatesAndEntitlementsStayTenantScoped()
    {
        var tenantA = Guid.CreateVersion7(); var tenantB = Guid.CreateVersion7(); var tenantC = Guid.CreateVersion7();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            foreach (var id in new[] { tenantA, tenantB, tenantC })
            {
                var tenant = new Tenant { Id = id, Identifier = $"matrix-{id:N}", DisplayName = $"Matrix {id:N}", HostName = $"{id:N}.invalid", DeploymentStampId = Guid.Parse("0194f700-0000-7000-8000-000000000001"), MaxUsers = 50, CreatedBy = "integration-vendor" };
                tenant.Approve("integration-vendor"); tenant.MarkAsActive();
                foreach(var module in id == tenantC ? new[] { "Core", "IAM" } : new[] { "Core", "IAM", "Payments" }) tenant.Modules.Add(new TenantModule { TenantId = id, ModuleKey = module, CreatedBy = "integration-vendor" });
                context.Tenants.Add(tenant);
            }
            await context.SaveChangesAsync();
        }
        using var vendor = Client(Guid.Parse("0194f700-0000-7000-8000-000000000001"), true);
        using var a = Client(tenantA); using var b = Client(tenantB); using var c = Client(tenantC);
        try
        {
            await Flag(vendor, Guid.Empty, false); await Flag(vendor, tenantA, true); await Flag(vendor, tenantC, true);
            Assert.Contains("/payments", await a.GetStringAsync("/api/v1/iam/admin/menus/navigation/Sidebar"), StringComparison.Ordinal);
            Assert.DoesNotContain("/payments", await b.GetStringAsync("/api/v1/iam/admin/menus/navigation/Sidebar"), StringComparison.Ordinal);
            Assert.DoesNotContain("/payments", await c.GetStringAsync("/api/v1/iam/admin/menus/navigation/Sidebar"), StringComparison.Ordinal);
            using var availablePayment = await a.GetAsync("/api/v1/shared/payments/capabilities"); Assert.Equal(HttpStatusCode.OK, availablePayment.StatusCode);
            using var deniedPayment = await b.GetAsync("/api/v1/shared/payments/capabilities"); Assert.Equal(HttpStatusCode.Forbidden, deniedPayment.StatusCode);
            var hiddenPermissions = await b.GetFromJsonAsync<AppResponse<PagedResponse<PermissionResponse, Guid>>>("/api/v1/iam/admin/permissions?pageSize=100");
            Assert.True(hiddenPermissions!.IsSuccess); Assert.DoesNotContain(hiddenPermissions.Data!.Items, x => x.Key.StartsWith("payments.", StringComparison.Ordinal));
            var templates = await a.GetFromJsonAsync<AppResponse<IReadOnlyList<CatalogueEntryResponse>>>("/api/v1/modules/available");
            var viewer = templates!.Data!.Single(x => x.Key == "payments-viewer");
            var roleName = "Scoped Payments Reader";
            var appliedA = await Post<AdminRoleListResponse>(a, "/api/v1/iam/admin/roles/apply-template", new ApplyRoleTemplateRequest(viewer.Key, roleName, viewer.Version));
            using var blockedTemplate = await b.PostAsJsonAsync("/api/v1/iam/admin/roles/apply-template", new ApplyRoleTemplateRequest(viewer.Key, roleName, viewer.Version));
            Assert.False((await blockedTemplate.Content.ReadFromJsonAsync<AppResponse<AdminRoleListResponse>>())!.IsSuccess);
            await Flag(vendor, Guid.Empty, true);
            var appliedB = await Post<AdminRoleListResponse>(b, "/api/v1/iam/admin/roles/apply-template", new ApplyRoleTemplateRequest(viewer.Key, roleName, viewer.Version));
            Assert.NotEqual(appliedA.Id, appliedB.Id);
            using var removed = await vendor.DeleteAsync($"/api/v1/control-plane/tenants/{tenantA}/modules/Payments"); Assert.True((await removed.Content.ReadFromJsonAsync<AppResponse<ST.SharedKernel.Features.ControlPlane.Tenants.Dtos.TenantResponse>>())!.IsSuccess);
            var hiddenRoles = await a.GetFromJsonAsync<AppResponse<IReadOnlyList<AdminRoleListResponse>>>("/api/v1/iam/admin/roles"); Assert.DoesNotContain(hiddenRoles!.Data!, role => role.Id == appliedA.Id);
            var visibleRoles = await b.GetFromJsonAsync<AppResponse<IReadOnlyList<AdminRoleListResponse>>>("/api/v1/iam/admin/roles"); Assert.Contains(visibleRoles!.Data!, role => role.Id == appliedB.Id);
            await Post<ST.SharedKernel.Features.ControlPlane.Tenants.Dtos.TenantResponse>(vendor, $"/api/v1/control-plane/tenants/{tenantA}/modules", new { ModuleKey = "Payments" });
            var restoredRoles = await a.GetFromJsonAsync<AppResponse<IReadOnlyList<AdminRoleListResponse>>>("/api/v1/iam/admin/roles"); Assert.Contains(restoredRoles!.Data!, role => role.Id == appliedA.Id);
            var navA = await a.GetFromJsonAsync<AppResponse<PagedResponse<MenuResponse, Guid>>>("/api/v1/iam/admin/menus?pageSize=100"); Assert.True(navA!.IsSuccess);
            var home = navA.Data!.Items.Single(x => x.Key == "dashboard");
            var change = new UpdateMenuRequest { Id = home.Id, Title = "Tenant A home", Url = home.Url, Icon = home.Icon, Placement = home.Placement, DisplayOrder = home.DisplayOrder, RequiredRoleNames = [], RequiredPermissionKeys = [], ExpectedVersion = 0 };
            using var saved = await a.PutAsJsonAsync($"/api/v1/iam/admin/menus/{home.Id}", change); var savedBody = await saved.Content.ReadFromJsonAsync<AppResponse<MenuResponse>>(); Assert.True(savedBody!.IsSuccess, await saved.Content.ReadAsStringAsync());
            var bHome = await b.GetFromJsonAsync<AppResponse<MenuResponse>>($"/api/v1/iam/admin/menus/{home.Id}"); Assert.Equal("Dashboard", bHome!.Data!.Title);
            using var stale = await a.PutAsJsonAsync($"/api/v1/iam/admin/menus/{home.Id}", change); Assert.False((await stale.Content.ReadFromJsonAsync<AppResponse<MenuResponse>>())!.IsSuccess);
            using var reset = await a.DeleteAsync("/api/v1/iam/admin/navigation/overrides/dashboard"); Assert.True((await reset.Content.ReadFromJsonAsync<AppResponse<bool>>())!.IsSuccess);
            var resetHome = await a.GetFromJsonAsync<AppResponse<MenuResponse>>($"/api/v1/iam/admin/menus/{home.Id}"); Assert.Equal("Dashboard", resetHome!.Data!.Title); Assert.False(resetHome.Data.IsOverride);
            var admin = navA.Data.Items.Single(x => x.Key == "admin-center"); var child = navA.Data.Items.Single(x => x.Key == "admin-menus");
            using var cycle = await a.PutAsJsonAsync($"/api/v1/iam/admin/menus/{admin.Id}", new UpdateMenuRequest { Id = admin.Id, Title = admin.Title, Url = admin.Url, Icon = admin.Icon, Placement = admin.Placement, ParentId = child.Id });
            Assert.False((await cycle.Content.ReadFromJsonAsync<AppResponse<MenuResponse>>())!.IsSuccess);
            using var hide = await a.PutAsJsonAsync($"/api/v1/iam/admin/menus/{admin.Id}", new UpdateMenuRequest { Id = admin.Id, Title = admin.Title, Url = admin.Url, Icon = admin.Icon, Placement = admin.Placement, IsActive = false }); Assert.True((await hide.Content.ReadFromJsonAsync<AppResponse<MenuResponse>>())!.IsSuccess);
            Assert.DoesNotContain("/admin/menus", await a.GetStringAsync("/api/v1/iam/admin/menus/navigation/AdminCenter"), StringComparison.Ordinal);
            Assert.Contains("/admin/menus", await b.GetStringAsync("/api/v1/iam/admin/menus/navigation/AdminCenter"), StringComparison.Ordinal);
        }
        finally { await Flag(vendor, Guid.Empty, true); }
    }
    private static async Task<T> Post<T>(HttpClient client, string path, object request)
    {
        using var response = await client.PostAsJsonAsync(path, request); var body = await response.Content.ReadFromJsonAsync<AppResponse<T>>(); Assert.True(body!.IsSuccess, await response.Content.ReadAsStringAsync()); return body.Data!;
    }
    private static async Task Flag(HttpClient client, Guid tenant, bool enabled)
    {
        using var response = await client.PutAsJsonAsync("/api/v1/control-plane/modules/Payments/flags", new SaveModuleFlagRequest(tenant, "enabled", "Payments availability", enabled)); var result = await response.Content.ReadFromJsonAsync<AppResponse<bool>>(); Assert.True(result!.IsSuccess, result.Message);
    }
    private HttpClient Client(Guid tenant, bool vendor = false)
    {
        var client = factory.CreateClient(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme); client.DefaultRequestHeaders.Add("X-Test-TenantId", tenant.ToString()); client.DefaultRequestHeaders.Add("X-Test-UserId", "integration-vendor");
        client.DefaultRequestHeaders.Add("X-Test-Permissions", vendor ? "control_plane.manage" : "menus.view,menus.create,menus.edit,menus.delete,roles.view,roles.create,roles.manage_permissions,payments.view");
        if(vendor) client.DefaultRequestHeaders.Add("X-Test-PlatformStaff", "true"); return client;
    }
}
