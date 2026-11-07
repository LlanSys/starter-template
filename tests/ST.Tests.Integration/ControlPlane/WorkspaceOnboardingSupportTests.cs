using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Support;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Tests.Integration.TestFixtures;
using Microsoft.AspNetCore.WebUtilities;
using OtpNet;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.SharedKernel.Features.ControlPlane.Demo;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
namespace ST.Tests.Integration.ControlPlane;

[Collection("Starter API")]
public sealed class WorkspaceOnboardingSupportTests(StarterTemplateWebApplicationFactory factory)
{
    private static readonly Guid Platform = Guid.Parse("0194f700-0000-7000-8000-000000000001");
    [Fact]
    public async Task InvitationMfaScopedSupportAndRevocationUseRealTenantSessions()
    {
        var a = Guid.CreateVersion7(); var b = Guid.CreateVersion7();
        using (var scope = factory.Services.CreateScope())
        {
            var cp = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            foreach (var id in new[] { a, b })
            {
                var t = new Tenant { Id = id, Identifier = $"onboard-{id:N}", DisplayName = "Onboarding test", HostName = $"{id:N}.invalid", DeploymentStampId = Platform, CreatedBy = "test-vendor", MaxUsers = 50 }; t.Approve("test-vendor", DateTimeOffset.UnixEpoch, "review-1"); t.MarkAsActive();
                foreach (var key in new[] { "Core", "IAM", "Payments" }) t.Modules.Add(new TenantModule { TenantId = id, ModuleKey = key, CreatedBy = "test-vendor" }); cp.Add(t);
            }
            await cp.SaveChangesAsync();
        }
        var vendorIdentity = await IdentityAsync(Platform, true); using var vendor = Bearer(vendorIdentity.Token);
        var email = $"workspace-{a:N}@example.test";
        var request = new InitializeWorkspaceRequest(email, "Tenant", "Owner", true);
        var setup = await Post<WorkspaceSetupResponse>(vendor, $"/api/v1/control-plane/tenants/{a}/workspace", request);
        Assert.Equal("Pending", setup.InvitationState);
        Assert.False(setup.EmailConfirmed);
        await Post<WorkspaceSetupResponse>(vendor, $"/api/v1/control-plane/tenants/{b}/workspace", request);
        await Post<WorkspaceSetupResponse>(vendor, $"/api/v1/control-plane/tenants/{a}/workspace", request with { SendInvitation = false });

        using var anonymous = factory.CreateClient(new() { HandleCookies = false });
        anonymous.DefaultRequestHeaders.Add("X-Tenant-Id", a.ToString());
        using var otherWorkspace = factory.CreateClient(new() { HandleCookies = false });
        otherWorkspace.DefaultRequestHeaders.Add("X-Tenant-Id", b.ToString());
        var (invitationId, token) = await ReadInvitationAsync(a, email);
        var inspect = new InspectAdministratorInvitationRequest(invitationId, token);
        await Post<AdministratorInvitationPreview>(anonymous, "/api/v1/workspaces/administrator-invitation/inspect", inspect);
        using (var wrongWorkspace = await otherWorkspace.PostAsJsonAsync("/api/v1/workspaces/administrator-invitation/inspect", inspect))
            Assert.False((await wrongWorkspace.Content.ReadFromJsonAsync<AppResponse<AdministratorInvitationPreview>>())!.IsSuccess);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            Assert.False(await db.Users.IgnoreQueryFilters().AnyAsync(user => user.Email == email));
        }

        await Post<bool>(vendor, $"/api/v1/control-plane/tenants/{a}/workspace/invitation/revoke", null);
        using (var revokedInvitation = await anonymous.PostAsJsonAsync("/api/v1/workspaces/administrator-invitation/inspect", inspect))
            Assert.False((await revokedInvitation.Content.ReadFromJsonAsync<AppResponse<AdministratorInvitationPreview>>())!.IsSuccess);
        await Post<bool>(vendor, $"/api/v1/control-plane/tenants/{a}/workspace/invitation/resend", null);
        (_, token) = await ReadInvitationAsync(a, email, token);

        const string password = "TenantOnboard42!";
        var acceptance = new AcceptAdministratorInvitationRequest(invitationId, token, password);
        var attempts = await Task.WhenAll(
            anonymous.PostAsJsonAsync("/api/v1/workspaces/administrator-invitation/accept", acceptance),
            anonymous.PostAsJsonAsync("/api/v1/workspaces/administrator-invitation/accept", acceptance));
        using (attempts[0])
        using (attempts[1])
        {
            Assert.Single(attempts, response => response.IsSuccessStatusCode);
            Assert.All(attempts, response => Assert.True((int)response.StatusCode < 500));
        }

        using (var replay = await anonymous.PostAsJsonAsync("/api/v1/workspaces/administrator-invitation/accept", acceptance))
            Assert.False((await replay.Content.ReadFromJsonAsync<AppResponse<bool>>())!.IsSuccess);

        var (otherInvitation, otherToken) = await ReadInvitationAsync(b, email);
        await Post<bool>(otherWorkspace, "/api/v1/workspaces/administrator-invitation/accept",
            new AcceptAdministratorInvitationRequest(otherInvitation, otherToken, password));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            var tenants = await db.Users.IgnoreQueryFilters().Where(user => user.Email == email).Select(user => user.TenantId).ToListAsync();
            Assert.Equal(2, tenants.Count);
            Assert.Contains(a, tenants);
            Assert.Contains(b, tenants);
        }

        var credentials = new { UserName = email, Password = password, RememberMe = false, DeviceFingerprint = "onboard-test" };
        var login = await Post<LoginResponse>(anonymous, "/api/v1/iam/auth/login", credentials);
        using var tenant = Bearer(login.Token);
        using (var blocked = await tenant.GetAsync("/api/v1/iam/admin/roles")) Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        var enrollment = await Post<TwoFactorSetupInfo>(tenant, "/api/v1/iam/users/totp/setup", null);
        var otp = new Totp(Base32Encoding.ToBytes(enrollment.ManualEntryKey));
        var verify = new VerifyOtpRequest { UserId = login.UserId, Code = otp.ComputeTotp(), DeviceFingerprint = "onboard-test" };
        var enrolled = await Post<VerifyOtpResponse>(tenant, "/api/v1/iam/users/totp/verify", verify);
        tenant.DefaultRequestHeaders.Authorization = new("Bearer", enrolled.Token);
        using (var early = await tenant.PostAsJsonAsync("/api/v1/workspaces/administrator-invitation/complete", new { mfaVerified = true }))
            Assert.Equal(HttpStatusCode.Forbidden, early.StatusCode);

        var challenge = await Post<LoginResponse>(anonymous, "/api/v1/iam/auth/login", credentials);
        Assert.True(challenge.Requires2FA);
        var mfa = await Post<VerifyOtpResponse>(anonymous, "/api/v1/iam/users/totp/verify",
            verify with { Code = otp.ComputeTotp(), ChallengeToken = challenge.Token });
        var beforeComplete = await Post<RefreshTokenResponse>(anonymous, "/api/v1/iam/auth/refresh-token", new RefreshTokenRequest(mfa.Token, mfa.RefreshToken));
        tenant.DefaultRequestHeaders.Authorization = new("Bearer", beforeComplete.Token);
        await Post<bool>(tenant, "/api/v1/workspaces/administrator-invitation/complete", null);
        var refreshed = await Post<RefreshTokenResponse>(anonymous, "/api/v1/iam/auth/refresh-token",
            new RefreshTokenRequest(beforeComplete.Token, beforeComplete.RefreshToken));
        tenant.DefaultRequestHeaders.Authorization = new("Bearer", refreshed.Token);
        using (var roles = await tenant.GetAsync("/api/v1/iam/admin/roles")) Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
        var status = await vendor.GetFromJsonAsync<AppResponse<WorkspaceSetupResponse>>($"/api/v1/control-plane/tenants/{a}/workspace");
        Assert.Equal("Accepted", status!.Data!.InvitationState);
        Assert.True(status.Data.MfaEnabled);
        var supportRequest = new SupportRequest(a, "Investigate role visibility", ["menus.view", "roles.view"], true, 15);
        var grant = (await Post<SupportResult>(vendor, "/api/v1/control-plane/support", supportRequest)).Grant;
        using (var early = await vendor.PostAsync($"/api/v1/control-plane/support/{grant.Id}/enter", null)) Assert.False((await early.Content.ReadFromJsonAsync<AppResponse<SupportResult>>())!.IsSuccess);
        var otherAdmin = await IdentityAsync(b, false); using var tenantB = Bearer(otherAdmin.Token);
        using (var cross = await tenantB.PostAsJsonAsync($"/api/v1/iam/support/{grant.Id}/approve", supportRequest)) Assert.False((await cross.Content.ReadFromJsonAsync<AppResponse<SupportResult>>())!.IsSuccess);
        await Post<SupportResult>(tenant, $"/api/v1/iam/support/{grant.Id}/approve", supportRequest);
        var entered = await Post<SupportResult>(vendor, $"/api/v1/control-plane/support/{grant.Id}/enter", null); var session = entered.Session!; using var support = Bearer(session.Token);
        var current = await support.GetFromJsonAsync<AppResponse<CurrentUserResponse>>("/api/v1/iam/auth/me"); Assert.Equal(grant.Id, current!.Data!.SupportGrantId); Assert.True(current.Data.IsReadOnlySupport); Assert.False(current.Data.IsPlatformStaff);
        using (var roles = await support.GetAsync("/api/v1/iam/admin/roles")) Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
        using (var denied = await support.GetAsync("/api/v1/iam/admin/users")) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var write = await support.PostAsJsonAsync("/api/v1/iam/admin/roles", new { Name = "Forbidden" })) Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        // Altered proxy roles must not expand the approved live scope.
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = a;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(); var proxy = await users.FindByIdAsync(session.UserId); Assert.True((await users.AddClaimAsync(proxy!, new Claim("permission", "users.view"))).Succeeded);
            var claims = await scope.ServiceProvider.GetRequiredService<IClaimsService>().GetUserClaimsAsync(proxy!, Guid.Parse(session.SessionId!)); claims.Add(new Claim(ClaimTypes.Role, "System Administrator"));
            support.DefaultRequestHeaders.Authorization = new("Bearer", await scope.ServiceProvider.GetRequiredService<IJwtService>().CreateTokenAsync(claims));
        }
        using (var escalated = await support.GetAsync("/api/v1/iam/admin/users")) Assert.Equal(HttpStatusCode.Forbidden, escalated.StatusCode);
        await Post<SupportResult>(tenant, $"/api/v1/iam/support/{grant.Id}/revoke", null);
        using (var revoked = await support.GetAsync("/api/v1/iam/admin/roles")) Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        using (var refresh = await anonymous.PostAsJsonAsync("/api/v1/iam/auth/refresh-token", new RefreshTokenRequest(session.Token, session.RefreshToken))) Assert.False((await refresh.Content.ReadFromJsonAsync<AppResponse<RefreshTokenResponse>>())!.IsSuccess);
        var second = (await Post<SupportResult>(vendor, "/api/v1/control-plane/support", supportRequest with { ReadOnly = false, PermissionKeys = ["menus.view", "menus.edit"] })).Grant;
        // Customer can reduce a write request to read-only, but cannot widen beyond the requested scope.
        await Post<SupportResult>(tenant, $"/api/v1/iam/support/{second.Id}/approve", supportRequest with { PermissionKeys = ["menus.view"], ReadOnly = true, DurationMinutes = 5 });
        var secondEntry = await Post<SupportResult>(vendor, $"/api/v1/control-plane/support/{second.Id}/enter", null); using var expired = Bearer(secondEntry.Session!.Token);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>(); var g = await db.SupportGrants.SingleAsync(x => x.Id == second.Id); g.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1); await db.SaveChangesAsync(); Assert.True(await db.SupportAudit.AnyAsync(x => x.GrantId == grant.Id && x.ActorId == vendorIdentity.UserId && x.Action.StartsWith("GET"))); }
        using (var denied = await expired.GetAsync("/api/v1/iam/admin/roles")) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var audit = await tenant.GetFromJsonAsync<AppResponse<IReadOnlyList<SupportAuditResponse>>>($"/api/v1/iam/support/{grant.Id}/audit"); Assert.True(audit!.IsSuccess); Assert.NotEmpty(audit.Data!);
        using (var foreignAudit = await tenantB.GetAsync($"/api/v1/iam/support/{grant.Id}/audit")) Assert.Equal(HttpStatusCode.NotFound, foreignAudit.StatusCode);
        var writeRequest = supportRequest with { ReadOnly = false, PermissionKeys = ["menus.view", "menus.edit"] };
        var writable = (await Post<SupportResult>(vendor, "/api/v1/control-plane/support", writeRequest)).Grant;
        await Post<SupportResult>(tenant, $"/api/v1/iam/support/{writable.Id}/approve", writeRequest);
        var writerSession = (await Post<SupportResult>(vendor, $"/api/v1/control-plane/support/{writable.Id}/enter", null)).Session!; using var writer = Bearer(writerSession.Token);
        var navigation = await writer.GetFromJsonAsync<AppResponse<PagedResponse<MenuResponse, Guid>>>("/api/v1/iam/admin/menus?pageSize=100"); var home = navigation!.Data!.Items.Single(x => x.Key == "dashboard");
        using (var updated = await writer.PutAsJsonAsync($"/api/v1/iam/admin/menus/{home.Id}", new UpdateMenuRequest { Id = home.Id, Title = "Supported workspace", Url = home.Url, Icon = home.Icon, Placement = home.Placement, IsActive = true, ExpectedVersion = home.OverrideVersion })) Assert.True(updated.IsSuccessStatusCode);
        using (var scope = factory.Services.CreateScope()) { scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = a; var db = scope.ServiceProvider.GetRequiredService<IamDBContext>(); Assert.Equal(vendorIdentity.UserId, (await db.NavigationOverrides.SingleAsync(x => x.Title == "Supported workspace")).CreatedBy); }
        var saved = await writer.GetFromJsonAsync<AppResponse<MenuResponse>>($"/api/v1/iam/admin/menus/{home.Id}");
        using (var changed = await writer.PutAsJsonAsync($"/api/v1/iam/admin/menus/{home.Id}", new UpdateMenuRequest { Id = home.Id, Title = "Supported again", Url = home.Url, Icon = home.Icon, Placement = home.Placement, IsActive = true, ExpectedVersion = saved!.Data!.OverrideVersion })) Assert.True(changed.IsSuccessStatusCode);
        using (var scope = factory.Services.CreateScope()) { scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = a; var db = scope.ServiceProvider.GetRequiredService<IamDBContext>(); Assert.Equal(vendorIdentity.UserId,(await db.NavigationOverrides.SingleAsync(x=>x.Title=="Supported again")).UpdatedBy); }
        await Post<SupportResult>(writer, $"/api/v1.0/iam/support/{writable.Id}/exit", null);
        using (var ended = await writer.GetAsync("/api/v1/iam/admin/menus")) Assert.Equal(HttpStatusCode.Forbidden, ended.StatusCode);
        var sourceGrant = (await Post<SupportResult>(vendor, "/api/v1/control-plane/support", supportRequest)).Grant; await Post<SupportResult>(tenant, $"/api/v1/iam/support/{sourceGrant.Id}/approve", supportRequest);
        var sourceSession = (await Post<SupportResult>(vendor, $"/api/v1/control-plane/support/{sourceGrant.Id}/enter", null)).Session!; using var sourceRevoked = Bearer(sourceSession.Token);
        using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(); var source = await users.FindByIdAsync(vendorIdentity.UserId); Assert.True((await users.RemoveClaimAsync(source!, new Claim("platform_staff", "true"))).Succeeded); }
        using (var blocked = await sourceRevoked.GetAsync("/api/v1/iam/admin/roles")) Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);

    }
    private async Task<(Guid Id, string Token)> ReadInvitationAsync(Guid tenantId, string email, string? previousToken = null)
    {
        using var mail = factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
        mail.BaseAddress = factory.MailpitApi;
        for (var attempt = 0; attempt < 24; attempt++)
        {
            using var summary = await mail.GetFromJsonAsync<JsonDocument>("/api/v1/messages");
            var messages = summary!.RootElement.GetProperty("messages").EnumerateArray()
                .Where(message => message.GetProperty("Subject").GetString() == "Your workspace administrator invitation"
                    && message.GetProperty("To").EnumerateArray().Any(address => address.GetProperty("Address").GetString() == email));
            foreach (var message in messages)
            {
                using var content = await mail.GetFromJsonAsync<JsonDocument>("/api/v1/message/" + message.GetProperty("ID").GetString());
                var html = content!.RootElement.GetProperty("HTML").GetString()!;
                var link = new Uri(WebUtility.HtmlDecode(Regex.Match(html, "href=\"([^\"]+)\"").Groups[1].Value));
                var query = QueryHelpers.ParseQuery(link.Query);
                var token = query["token"].ToString();
                if (query["workspace"].ToString() == $"onboard-{tenantId:N}" && token != previousToken)
                {
                    return (Guid.Parse(query["invitation"].ToString()), token);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new InvalidOperationException("The administrator invitation did not arrive in Mailpit.");
    }

    [Fact]
    public async Task DemoResetEndsDemoSessionsWithoutChangingCustomerNavigation()
    {
        var customer = Guid.CreateVersion7(); await factory.RegisterTenantAsync(customer);
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = customer;
            var db = scope.ServiceProvider.GetRequiredService<IamDBContext>(); db.NavigationOverrides.Add(new NavigationOverride { PageKey = "dashboard", Title = "Customer dashboard", CreatedBy = "customer-admin" }); await db.SaveChangesAsync();
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>(); var reserved = DemoWorkspace.TenantId;
            if (!await db.Tenants.AnyAsync(x => x.Id == reserved))
            {
                var reservedTenant = new Tenant { Id = reserved, Identifier = DemoWorkspace.Identifier, DisplayName = "Vendor demo", HostName = "vendor-demo.invalid", DeploymentStampId = Platform, IsDemo = true, CreatedBy = "test-vendor", MaxUsers = 100 }; reservedTenant.Approve("test-vendor", DateTimeOffset.UnixEpoch, "review-1"); reservedTenant.MarkAsActive();
                foreach (var key in new[] { "Core", "IAM" }) reservedTenant.Modules.Add(new TenantModule { TenantId = reserved, ModuleKey = key, CreatedBy = "test-vendor" }); db.Add(reservedTenant); await db.SaveChangesAsync();
            }
            else { db.RemoveRange(await db.TenantModules.Where(x => x.TenantId == reserved && x.ModuleKey == "Payments").ToListAsync()); await db.SaveChangesAsync(); }
        }
        var identity = await IdentityAsync(Platform, true); using var vendor = Bearer(identity.Token);
        var entry = await Post<LoginResponse>(vendor, "/api/v1/control-plane/demo/enter", null); using var demo = Bearer(entry.Token);
        var nav = await demo.GetFromJsonAsync<AppResponse<PagedResponse<MenuResponse, Guid>>>("/api/v1/iam/admin/menus?pageSize=100");
        var home = nav!.Data!.Items.Single(x => x.Key == "dashboard");
        using (var edit = await demo.PutAsJsonAsync($"/api/v1/iam/admin/menus/{home.Id}", new UpdateMenuRequest { Id = home.Id, Title = "Customized demo", Url = home.Url, Icon = home.Icon, Placement = home.Placement, IsActive = true, ExpectedVersion = home.OverrideVersion })) Assert.True(edit.IsSuccessStatusCode);
        await Post<bool>(vendor, "/api/v1/control-plane/demo/reset", null);
        using (var old = await demo.GetAsync("/api/v1/iam/auth/me")) Assert.False(old.IsSuccessStatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = customer;
            var db = scope.ServiceProvider.GetRequiredService<IamDBContext>(); Assert.True(await db.NavigationOverrides.AnyAsync(x => x.Title == "Customer dashboard"));
        }
        var fresh = await Post<LoginResponse>(vendor, "/api/v1/control-plane/demo/enter", null); using var freshDemo = Bearer(fresh.Token);
        var restored = await freshDemo.GetFromJsonAsync<AppResponse<PagedResponse<MenuResponse, Guid>>>("/api/v1/iam/admin/menus?pageSize=100"); Assert.Equal("Dashboard", restored!.Data!.Items.Single(x => x.Key == "dashboard").Title);
    }
    [Fact]
    public async Task TenantCreationApprovalAndModuleEditsAreVisibleImmediately()
    {
        var identity=await IdentityAsync(Platform,true);using var vendor=Bearer(identity.Token);
        await vendor.GetFromJsonAsync<AppResponse<List<TenantResponse>>>("/api/v1/control-plane/tenants");
        var key=$"visible-{Guid.CreateVersion7():N}";
        var created=await Post<TenantResponse>(vendor,"/api/v1/control-plane/tenants",new CreateTenantRequest { Identifier=key,DisplayName="Visible customer",HostName=key+".invalid",SubscriptionTier="Free",DeploymentStampId=Platform });
        var listed=await vendor.GetFromJsonAsync<AppResponse<List<TenantResponse>>>("/api/v1/control-plane/tenants");Assert.Contains(listed!.Data!,x=>x.Id==created.Id && x.Status=="PendingApproval");
        await vendor.GetFromJsonAsync<AppResponse<TenantResponse>>($"/api/v1/control-plane/tenants/{created.Id}");
        await Post<TenantResponse>(vendor,$"/api/v1/control-plane/tenants/{created.Id}/approve", new ApproveTenantRequest("review-created"));
        var active=await vendor.GetFromJsonAsync<AppResponse<TenantResponse>>($"/api/v1/control-plane/tenants/{created.Id}");Assert.Equal("Active",active!.Data!.Status);
        foreach(var module in new[]{"Core","IAM","Payments"})await Post<TenantResponse>(vendor,$"/api/v1/control-plane/tenants/{created.Id}/modules",new{ModuleKey=module});
        var assigned=await vendor.GetFromJsonAsync<AppResponse<TenantResponse>>($"/api/v1/control-plane/tenants/{created.Id}");Assert.Contains("Payments",assigned!.Data!.EnabledModules);
        using(var remove=await vendor.DeleteAsync($"/api/v1/control-plane/tenants/{created.Id}/modules/Payments"))Assert.True(remove.IsSuccessStatusCode);
        var removed=await vendor.GetFromJsonAsync<AppResponse<TenantResponse>>($"/api/v1/control-plane/tenants/{created.Id}");Assert.DoesNotContain("Payments",removed!.Data!.EnabledModules);
    }
    private async Task<(string Token, string UserId)> IdentityAsync(Guid tenant, bool staff)
    {
        await using var scope = factory.Services.CreateAsyncScope(); scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenant;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(); var name = $"support-{Guid.CreateVersion7():N}"; var user = AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, tenant, name, name + "@example.test", "test"); user.IsActive = true; user.EmailConfirmed = true; user.TwoFactorEnabled = true; Assert.True((await users.CreateAsync(user)).Succeeded);
        Assert.True((await users.AddClaimsAsync(user, staff ? [new("platform_staff", "true"), new("permission", "control_plane.manage"), new("permission", "control_plane.approve_kyc")] : [new("permission", "support.approve")])).Succeeded);
        var sessions = scope.ServiceProvider.GetRequiredService<ISessionService>(); var session = await sessions.CreateSessionAsync(user.Id, Guid.CreateVersion7(), "127.0.0.1", "test"); Assert.True(session.IsSuccess);
        var claims = await scope.ServiceProvider.GetRequiredService<IClaimsService>().GetUserClaimsAsync(user, session.Data); claims.Add(new Claim("amr", "mfa")); return (await scope.ServiceProvider.GetRequiredService<IJwtService>().CreateTokenAsync(claims), user.Id);
    }
    private HttpClient Bearer(string token) { var c = factory.CreateClient(new() { HandleCookies = false }); c.DefaultRequestHeaders.Authorization = new("Bearer", token); return c; }
    private static async Task<T> Post<T>(HttpClient c, string path, object? request)
    {
        using var response = request is null ? await c.PostAsync(path, null) : await c.PostAsJsonAsync(path, request); if (!response.IsSuccessStatusCode)
        {
            using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>(); Assert.Fail($"{(int)response.StatusCode}: {(problem!.RootElement.TryGetProperty("detail", out var detail) ? detail.GetString() : "Request failed")}");
        }
        var result = await response.Content.ReadFromJsonAsync<AppResponse<T>>(); Assert.True(result!.IsSuccess, result.Message); return result.Data!;
    }
}
