using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Demo;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Tests.Integration.TestFixtures;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.Tests.Integration.ControlPlane;

[Collection("Starter API")]
public sealed class VendorWorkspaceTests(StarterTemplateWebApplicationFactory factory)
{
    private static readonly Guid SourceTenant = Guid.Parse("0194f700-0000-7000-8000-000000000001");

    [Fact]
    public async Task VendorCatalogueAndDemoUseRealSessionsWithoutPlatformPrivilegeLeak()
    {
        string actorId;
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var actor = AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, SourceTenant, $"vendor-{Guid.CreateVersion7()}", $"vendor-{Guid.CreateVersion7()}@example.test", "test-actor");
            actor.IsActive = true; actor.EmailConfirmed = true; actor.TwoFactorEnabled = true;
            Assert.True((await users.CreateAsync(actor)).Succeeded);
            Assert.True((await users.AddClaimsAsync(actor, [new Claim("platform_staff", "true"), new Claim("permission", "control_plane.manage")])).Succeeded);
            actorId = actor.Id;
        }
        using var vendor = Client(actorId, true);
        var catalogue = await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<ModuleResponse>>>("/api/v1/control-plane/modules");
        Assert.True(catalogue!.IsSuccess);
        Assert.Contains(catalogue.Data!, module => module.Key == "IAM" && module.IsImplemented);
        var moduleKey = $"future-{Guid.CreateVersion7():N}";
        using var draftResponse = await vendor.PostAsJsonAsync("/api/v1/control-plane/modules", new SaveModuleRequest(moduleKey, "Future capability", "Not implemented"));
        var draft = await draftResponse.Content.ReadFromJsonAsync<AppResponse<ModuleResponse>>();
        Assert.True(draft!.IsSuccess);
        Assert.False(draft.Data!.IsImplemented);
        using var unavailable = await vendor.PostAsJsonAsync($"/api/v1/control-plane/tenants/{SourceTenant}/modules", new { ModuleKey = moduleKey });
        Assert.False((await unavailable.Content.ReadFromJsonAsync<AppResponse<TenantResponse>>())!.IsSuccess);
        using var tenantAdmin = Client(actorId, false);
        using var deniedCatalogue = await tenantAdmin.GetAsync("/api/v1/control-plane/modules");
        Assert.Equal(HttpStatusCode.Forbidden, deniedCatalogue.StatusCode);
        using var entered = await vendor.PostAsync("/api/v1/control-plane/demo/enter", null);
        var login = await entered.Content.ReadFromJsonAsync<AppResponse<LoginResponse>>();
        Assert.True(login!.IsSuccess, await entered.Content.ReadAsStringAsync());
        Assert.NotEqual(actorId, login.Data!.UserId);
        using var demo = factory.CreateClient();
        demo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Data.Token);
        using var denied = await demo.GetAsync("/api/v1/control-plane/modules");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var profile = await demo.GetFromJsonAsync<AppResponse<CurrentUserResponse>>("/api/v1/iam/auth/me");
        Assert.True(profile!.IsSuccess);
        Assert.Equal(DemoWorkspace.TenantId, profile.Data!.TenantId);
        Assert.True(profile.Data.IsDemoWorkspace);
        Assert.False(profile.Data.IsPlatformStaff);
        var navigation = await demo.GetAsync("/api/v1/iam/admin/menus/navigation/Sidebar");
        Assert.Equal(HttpStatusCode.OK, navigation.StatusCode);
        Assert.DoesNotContain("/control-panel", await navigation.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        var showcase = catalogue.Data!.Single(module => module.Key == "Showcase");
        using var disabledModule = await vendor.PutAsJsonAsync($"/api/v1/control-plane/modules/{showcase.Id}", new SaveModuleRequest(showcase.Key, showcase.Name, showcase.Description, false));
        disabledModule.EnsureSuccessStatusCode();
        using var hiddenNavigation = await demo.GetAsync("/api/v1/iam/admin/menus/navigation/Sidebar");
        Assert.DoesNotContain("/overview", await hiddenNavigation.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Contains("/payments", await hiddenNavigation.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using var enabledModule = await vendor.PutAsJsonAsync($"/api/v1/control-plane/modules/{showcase.Id}", new SaveModuleRequest(showcase.Key, showcase.Name, showcase.Description, true));
        enabledModule.EnsureSuccessStatusCode();
        using var restoredNavigation = await demo.GetAsync("/api/v1/iam/admin/menus/navigation/Sidebar");
        Assert.Contains("/overview", await restoredNavigation.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Contains("/payments", await restoredNavigation.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        var payments = catalogue.Data!.Single(module => module.Key == "Payments");
        using var disabledPayments = await vendor.PostAsJsonAsync($"/api/v1/control-plane/modules/{payments.Id}/lifecycle", new ModuleLifecycleRequest(ModuleLifecycleAction.Disable));
        Assert.True((await disabledPayments.Content.ReadFromJsonAsync<AppResponse<ModuleResponse>>())!.IsSuccess);
        using var hiddenPayments = await demo.GetAsync("/api/v1/iam/admin/menus/navigation/Sidebar");
        Assert.DoesNotContain("/payments", await hiddenPayments.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using var deniedPaymentsApi = await demo.GetAsync("/api/v1/shared/payments/capabilities");
        Assert.Equal(HttpStatusCode.Forbidden, deniedPaymentsApi.StatusCode);
        using var enabledPayments = await vendor.PostAsJsonAsync($"/api/v1/control-plane/modules/{payments.Id}/lifecycle", new ModuleLifecycleRequest(ModuleLifecycleAction.Enable));
        Assert.True((await enabledPayments.Content.ReadFromJsonAsync<AppResponse<ModuleResponse>>())!.IsSuccess);
        using var retiredDraft = await vendor.PostAsJsonAsync($"/api/v1/control-plane/modules/{draft.Data.Id}/lifecycle", new ModuleLifecycleRequest(ModuleLifecycleAction.Retire));
        Assert.True((await retiredDraft.Content.ReadFromJsonAsync<AppResponse<ModuleResponse>>())!.Data!.IsRetired);
        using var reviveDraft = await vendor.PostAsJsonAsync($"/api/v1/control-plane/modules/{draft.Data.Id}/lifecycle", new ModuleLifecycleRequest(ModuleLifecycleAction.Enable));
        Assert.False((await reviveDraft.Content.ReadFromJsonAsync<AppResponse<ModuleResponse>>())!.IsSuccess);
        var iam = catalogue.Data!.Single(module => module.Key == "IAM");
        using var disableEssential = await vendor.PostAsJsonAsync($"/api/v1/control-plane/modules/{iam.Id}/lifecycle", new ModuleLifecycleRequest(ModuleLifecycleAction.Disable));
        Assert.False((await disableEssential.Content.ReadFromJsonAsync<AppResponse<ModuleResponse>>())!.IsSuccess);
        using var blockedPayment = await demo.PostAsJsonAsync("/api/v1/shared/payments/checkout", new { });
        Assert.Equal(HttpStatusCode.Forbidden, blockedPayment.StatusCode);
        using var repeated = await vendor.PostAsync("/api/v1/control-plane/demo/enter", null);
        var repeatedLogin = await repeated.Content.ReadFromJsonAsync<AppResponse<LoginResponse>>();
        Assert.True(repeatedLogin!.IsSuccess);
        Assert.Equal(login.Data.UserId, repeatedLogin.Data!.UserId);
        demo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", repeatedLogin.Data.Token);
        using var viewerEntry = await vendor.PostAsync("/api/v1/control-plane/demo/enter?readOnly=true", null);
        var viewerLogin = await viewerEntry.Content.ReadFromJsonAsync<AppResponse<LoginResponse>>();
        Assert.True(viewerLogin!.IsSuccess, await viewerEntry.Content.ReadAsStringAsync());
        using var viewer = factory.CreateClient();
        viewer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerLogin.Data!.Token);
        var viewerProfile = await viewer.GetFromJsonAsync<AppResponse<CurrentUserResponse>>("/api/v1/iam/auth/me");
        Assert.True(viewerProfile!.Data!.IsReadOnlyDemo);
        using var forbiddenWrite = await viewer.PostAsJsonAsync("/api/v1/iam/admin/roles", new { Name = "Must not create" });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenWrite.StatusCode);
        using var anonymous = factory.CreateClient();
        using var refreshResponse = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/refresh-token", new RefreshTokenRequest(repeatedLogin.Data!.Token, repeatedLogin.Data.RefreshToken));
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AppResponse<RefreshTokenResponse>>();
        Assert.True(refreshed!.IsSuccess, await refreshResponse.Content.ReadAsStringAsync());
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            Assert.True(await context.Users.IgnoreQueryFilters().AnyAsync(user => user.Id == login.Data.UserId && user.TenantId == DemoWorkspace.TenantId));
            Assert.False(await context.UserClaims.AnyAsync(claim => claim.UserId == login.Data.UserId && claim.ClaimType == "platform_staff"));
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var actor = await users.FindByIdAsync(actorId);
            Assert.True((await users.RemoveClaimAsync(actor!, new Claim("platform_staff", "true"))).Succeeded);
        }
        using var revokedRefresh = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/refresh-token", new RefreshTokenRequest(refreshed.Data!.Token, refreshed.Data.RefreshToken));
        var revokedRefreshBody = await revokedRefresh.Content.ReadFromJsonAsync<AppResponse<RefreshTokenResponse>>();
        Assert.False(revokedRefreshBody!.IsSuccess);
        using var revoked = await demo.GetAsync("/api/v1/iam/admin/menus/navigation/Sidebar");
        Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
    }

    private HttpClient Client(string userId, bool staff)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-TenantId", SourceTenant.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Permissions", "control_plane.manage");
        if (staff) client.DefaultRequestHeaders.Add("X-Test-PlatformStaff", "true");
        return client;
    }
}
