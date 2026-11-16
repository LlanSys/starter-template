using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using MediatR;
using ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Staff;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Tests.Integration.ControlPlane;

public sealed partial class PlatformIamSignInTests
{
    [Fact]
    public async Task StaffProductWorkflowSeparatesDutiesAuditsAndRevokesRealSessions()
    {
        using var app = CreateHost();
        var identity = await CreateIdentityAsync(
            app,
            Tenant.PlatformTenantId,
            "StaffManager",
            true,
            string.Join(',', PlatformPermissions.All)
        );
        using var manager = Bearer(app, await SignInWithAuthenticatorAsync(
            app,
            Tenant.PlatformTenantId,
            identity.Name,
            true
        ));
        var name = $"staff-{Guid.CreateVersion7():N}";
        var profile = PlatformStaffProfiles.All.Single(p => p.Key == "kyc-collector");
        var added = await PostAsync<PlatformStaffResponse>(
            manager,
            "/api/v1/control-plane/staff",
            new CreatePlatformStaffRequest(
                name,
                $"{name}@example.test",
                "QA",
                "Collector",
                profile.PermissionKeys.ToArray(),
                "Staff workflow test"
            )
        );
        Assert.True(added.IsStaff);
        Assert.False(added.EmailConfirmed);
        Assert.False(added.MfaEnabled);
        await PostAsync<bool>(
            manager,
            $"/api/v1/control-plane/staff/{added.UserId}/invitation",
            new PlatformStaffInvitationRequest(added.Revision, "Issue initial invitation")
        );
        // Read an identity-provider invitation token inside this disposable test host; acceptance goes through the real public endpoint.
        string firstToken;
        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByIdAsync(added.UserId))!;
            // Product invitation purpose is versioned; use the same implementation as its adapter.
            firstToken = await users.GenerateUserTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                AccountInvitation.Purpose(user)
            );
        }

        using var anonymous = app.CreateClient(new() { HandleCookies = false });
        anonymous.DefaultRequestHeaders.Add("X-Tenant-Id", Tenant.PlatformTenantId.ToString());
        await PostAsync<bool>(
            anonymous,
            "/api/v1/iam/auth/invitation/accept",
            new { UserId = added.UserId, Token = firstToken, Password, ConfirmPassword = Password }
        );
        using var collector = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, name, true));
        await ExpectAsync(collector, "/api/v1/control-plane/staff", false);
        using var createDenied = await collector.PostAsJsonAsync("/api/v1/control-plane/staff", new CreatePlatformStaffRequest(
            "denied",
            "denied@example.test",
            "Denied",
            "User",
            [],
            "Must deny"
        ));
        Assert.Equal(HttpStatusCode.Forbidden, createDenied.StatusCode);
        var listing = await manager.GetFromJsonAsync<AppResponse<PlatformStaffPageResponse>>("/api/v1/control-plane/staff?search=" + name);
        var current = Assert.Single(listing!.Data!.Items);
        using var changed = await manager.PutAsJsonAsync($"/api/v1/control-plane/staff/{current.UserId}", new UpdatePlatformStaffRequest(
            current.Revision,
            true,
            false,
            current.PermissionKeys.ToArray(),
            "Deactivate collector"
        ));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        using var revoked = await collector.GetAsync("/api/v1/control-plane/tenants");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using var stale = await manager.PutAsJsonAsync($"/api/v1/control-plane/staff/{current.UserId}", new UpdatePlatformStaffRequest(
            current.Revision,
            true,
            true,
            current.PermissionKeys.ToArray(),
            "Stale edit"
        ));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, stale.StatusCode);
        var history = await manager.GetFromJsonAsync<AppResponse<PlatformStaffAuditResponse[]>>($"/api/v1/control-plane/staff/{current.UserId}/history");
        Assert.Equal(3, history!.Data!.Length);
        Assert.All(history.Data, h => Assert.Equal(identity.UserId, h.ActorId));
        Assert.Contains(history.Data, h => h.Action == "AccessChanged" && !h.CurrentActive);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
            var db = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            var audit = await db.PlatformStaffChanges.FirstAsync(e => e.UserId == current.UserId);
            Assert.Equal(Tenant.PlatformTenantId, audit.TenantId);
            db.PlatformStaffChanges.Remove(audit);
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        using var disable = await manager.PostAsync("/api/v1/iam/users/totp/disable", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, disable.StatusCode);
    }

    [Fact]
    public async Task StaffManagerCannotDelegateUnheldGrantsOrTargetCustomerAccounts()
    {
        using var app = CreateHost();
        var identity = await CreateIdentityAsync(
            app,
            Tenant.PlatformTenantId,
            "LimitedStaffManager",
            true,
            $"{PlatformPermissions.ViewPlatformStaff},{PlatformPermissions.ManagePlatformStaff}"
        );
        using var manager = Bearer(app, await SignInWithAuthenticatorAsync(
            app,
            Tenant.PlatformTenantId,
            identity.Name,
            true
        ));
        using var denied = await manager.PostAsJsonAsync("/api/v1/control-plane/staff", new CreatePlatformStaffRequest(
            "excessive",
            "excessive@example.test",
            "QA",
            "Denied",
            [PlatformPermissions.ApproveKyc],
            "Cannot delegate"
        ));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var other = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(other);
        var customer = await CreateIdentityAsync(app, other, "Customer", false, "");
        using var outside = await manager.PutAsJsonAsync($"/api/v1/control-plane/staff/{customer.UserId}", new UpdatePlatformStaffRequest(
            "probe",
            true,
            true,
            [PlatformPermissions.ViewPlatformStaff],
            "Cross-workspace probe"
        ));
        Assert.Equal(HttpStatusCode.NotFound, outside.StatusCode);
        using var history = await manager.GetAsync($"/api/v1/control-plane/staff/{customer.UserId}/history");
        Assert.Equal(HttpStatusCode.NotFound, history.StatusCode);
    }

    [Fact]
    public async Task ConcurrentManagerRemovalsKeepOneUsableManagerAndGenericIamCannotBypass()
    {
        using var app = CreateHost();
        var keys = new[]
        {
            PlatformPermissions.ViewPlatformStaff,
            PlatformPermissions.ManagePlatformStaff
        };
        var first = await CreateIdentityAsync(
            app,
            Tenant.PlatformTenantId,
            "FirstStaffOwner",
            true,
            string.Join(',', keys)
        );
        var second = await CreateIdentityAsync(
            app,
            Tenant.PlatformTenantId,
            "SecondStaffOwner",
            true,
            string.Join(',', keys)
        );
        using var a = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, first.Name, true));
        using var b = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, second.Name, true));
        var removed = new List<string>();
        try
        {
            // This fixture is shared sequentially. Temporarily remove other test managers, then restore in finally.
            await using (var scope = app.Services.CreateAsyncScope())
            {
                scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
                var db = scope.ServiceProvider.GetRequiredService<IamDBContext>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var ids = await db.UserClaims.Where(c => c.ClaimType == "permission" && c.ClaimValue == PlatformPermissions.ManagePlatformStaff && c.UserId != first.UserId && c.UserId != second.UserId).Select(c => c.UserId).Distinct().ToArrayAsync();
                foreach (var id in ids)
                {
                    var user = (await users.FindByIdAsync(id))!;
                    Assert.True((await users.RemoveClaimAsync(user, new Claim("permission", PlatformPermissions.ManagePlatformStaff))).Succeeded);
                    removed.Add(id);
                }
            }

            var aList = await a.GetFromJsonAsync<AppResponse<PlatformStaffPageResponse>>("/api/v1/control-plane/staff?search=" + first.Name);
            var bList = await b.GetFromJsonAsync<AppResponse<PlatformStaffPageResponse>>("/api/v1/control-plane/staff?search=" + second.Name);
            var one = Assert.Single(aList!.Data!.Items);
            var two = Assert.Single(bList!.Data!.Items);
            var results = await Task.WhenAll(a.PutAsJsonAsync($"/api/v1/control-plane/staff/{one.UserId}", new UpdatePlatformStaffRequest(
                one.Revision,
                true,
                false,
                keys,
                "Concurrent self-removal A"
            )), b.PutAsJsonAsync($"/api/v1/control-plane/staff/{two.UserId}", new UpdatePlatformStaffRequest(
                two.Revision,
                true,
                false,
                keys,
                "Concurrent self-removal B"
            )));
            Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
            Assert.Single(results, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
            foreach (var result in results)
            {
                result.Dispose();
            }

            var iamIdentity = await CreateIdentityAsync(
                app,
                Tenant.PlatformTenantId,
                "OrdinaryIamOperator",
                false,
                "users.edit,users.deactivate,users.manage_roles,users.manage_permissions"
            );
            using var iam = Bearer(app, await SignInWithAuthenticatorAsync(
                app,
                Tenant.PlatformTenantId,
                iamIdentity.Name,
                false
            ));
            foreach (var target in new[]
            {
                first.UserId,
                second.UserId
            }

            )
            {
                using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/iam/admin/users/{target}/deactivate")
                {
                    Content = JsonContent.Create(new DeactivateUserRequest("Must use protected staff workflow"))
                };
                using var denied = await iam.SendAsync(request);
                Assert.Equal(HttpStatusCode.UnprocessableEntity, denied.StatusCode);
                using var roles = await iam.PutAsJsonAsync($"/api/v1/iam/admin/users/{target}/roles", new UpdateUserRolesRequest([]));
                Assert.Equal(HttpStatusCode.UnprocessableEntity, roles.StatusCode);
                using var permissions = await iam.PutAsJsonAsync($"/api/v1/iam/admin/users/{target}/permissions", new UpdateUserPermissionsRequest([]));
                Assert.Equal(HttpStatusCode.UnprocessableEntity, permissions.StatusCode);
                using var enrollment = await iam.PostAsync($"/api/v1/iam/users/totp/{target}/setup", null);
                Assert.True(enrollment.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.UnprocessableEntity);
            }
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            foreach (var id in removed)
            {
                Assert.True((await users.AddClaimAsync((await users.FindByIdAsync(id))!, new Claim("permission", PlatformPermissions.ManagePlatformStaff))).Succeeded);
            }
        }
    }

    [Fact]
    public async Task AuthenticatorManagementLimitsArePerUserAndExhaustionDoesNotQueue()
    {
        using var app = CreateHost();
        var first = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "MfaBudgetA", false, "");
        var second = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "MfaBudgetB", false, "");
        var firstToken = await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, first.Name, false);
        var secondToken = await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, second.Name, false);
        using var limited = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:TwoFactorPolicy:PermitLimit"] = "1", ["RateLimiting:TwoFactorPolicy:QueueLimit"] = "0", ["RateLimiting:TwoFactorPolicy:WindowSeconds"] = "300" })));
        using var a = Bearer(limited, firstToken);
        using var b = Bearer(limited, secondToken);
        using var allowedA = await a.GetAsync($"/api/v1/iam/users/totp/{first.UserId}/status");
        Assert.Equal(HttpStatusCode.OK, allowedA.StatusCode);
        using var exhausted = await a.GetAsync($"/api/v1/iam/users/totp/{first.UserId}/status").WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(HttpStatusCode.TooManyRequests, exhausted.StatusCode);
        Assert.NotNull(exhausted.Headers.RetryAfter);
        using var allowedB = await b.GetAsync($"/api/v1/iam/users/totp/{second.UserId}/status");
        Assert.Equal(HttpStatusCode.OK, allowedB.StatusCode);
    }
}
