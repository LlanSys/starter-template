using Microsoft.EntityFrameworkCore;
using ST.Application.Features.Shared.WorkTasks;
using ST.Domain.Features.ControlPlane.WorkTasks.Entities;
using ST.SharedKernel.Features.Shared.Dashboard;
using ST.Domain.Features.ControlPlane.Support.Entities;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

[Collection("Starter API")]
public sealed partial class PlatformIamSignInTests(StarterTemplateWebApplicationFactory factory)
{
    private const string Password = "MatrixCertification42!";
    private const string Manage = "control_plane.view_tenants";
    private const string Approve = "control_plane.view_kyc";

    [Theory]
    [InlineData("AdministratorLabel", false, "", false, false, false, false)]
    [InlineData("StaffOnly", true, "", false, false, false, false)]
    [InlineData("PermissionsWithoutStaff", false, "control_plane.view_tenants,control_plane.view_kyc,control_plane.approve_kyc", false, false, false, false)]
    [InlineData("ReservedRoleClaims", true, "control_plane.view_tenants,control_plane.view_kyc,control_plane.approve_kyc", false, false, false, false)]
    [InlineData("PlatformOnboardingManager", true, "control_plane.view_tenants,control_plane.manage_tenant_administrators", true, false, false, false)]
    [InlineData("PlatformReader", true, "control_plane.view_tenants", true, false, false, false)]
    [InlineData("LegacyGrant", true, "control_plane.manage", false, false, false, false)]
    [InlineData("KycReader", true, "control_plane.view_kyc", false, false, false, false)]
    [InlineData("KycOnly", true, "control_plane.view_kyc,control_plane.approve_kyc", false, true, false, false)]
    [InlineData("KycReviewer", true, "control_plane.view_tenants,control_plane.view_kyc,control_plane.approve_kyc", true, true, false, false)]
    [InlineData("KycCollector", true, "control_plane.view_kyc,control_plane.upload_kyc_documents,control_plane.manage_kyc_submissions", false, false, false, false)]
    [InlineData("RetentionOperator", true, "control_plane.view_kyc,control_plane.manage_kyc_retention,control_plane.manage_kyc_holds", false, false, false, false)]
    [InlineData("IamReader", true, "users.view,roles.view", false, false, true, false)]
    [InlineData("PlatformAndIamOperator", true, "control_plane.view_tenants,control_plane.view_kyc,control_plane.approve_kyc,users.view,roles.view,roles.create", true, true, true, true)]
    [InlineData("ApprovalWithoutPlatformManagement", true, "control_plane.view_kyc,control_plane.approve_kyc", false, true, false, false)]
    public async Task PasswordAndAuthenticatorSignInEnforcesRolePermissionsAndNavigation(
        string scenario, bool staff, string permissions, bool platform, bool approval, bool iamRead, bool roleWrite)
    {
        using var app = CreateHost();
        var identity = await CreateIdentityAsync(app, Tenant.PlatformTenantId, scenario, staff, permissions);
        var pending = await PendingTenantAsync(app);
        var token = await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, identity.Name, staff);
        using var client = Bearer(app, token);
        var verifiedProfile = await client.GetFromJsonAsync<AppResponse<CurrentUserResponse>>("/api/v1/iam/auth/me");
        Assert.True(verifiedProfile!.Data!.HasMfaProof);

        var attention = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>(
            "/api/v1/dashboard/attention");

        Assert.True(attention!.IsSuccess, attention.Message);
        var canActOnKyc = staff && scenario != "ReservedRoleClaims"
            && permissions.Split(',').Contains(PlatformPermissions.ViewKyc)
            && permissions.Split(',').Contains(PlatformPermissions.ApproveKyc);

        Assert.Equal(canActOnKyc, attention.Data!.Any(item => item.Key == "kyc-review"));
        Assert.All(attention.Data, item =>
        {
            Assert.True(item.Count > 0);
            Assert.False(string.IsNullOrWhiteSpace(item.Reason));
            Assert.False(string.IsNullOrWhiteSpace(item.ActionLabel));
            Assert.Equal(DashboardAttentionKind.TeamAction, item.Kind);
        });

        if (!platform)
        {
            Assert.DoesNotContain(attention.Data, item => item.Key is "provisioning-failed" or "invitation-delivery");
        }
        await ExpectAsync(client, "/api/v1/control-plane/tenants", platform);
        await ExpectAsync(client, "/api/v1/control-plane/tenants/dashboard", platform);

        if (platform)
        {
            var dashboard = await client.GetFromJsonAsync<AppResponse<PlatformDashboardResponse>>(
                "/api/v1/control-plane/tenants/dashboard");

            Assert.True(dashboard!.IsSuccess);
            Assert.True(dashboard.Data!.CustomerTenants > 0);
            Assert.True(dashboard.Data.AwaitingKyc > 0);

            if (permissions.Split(',').Contains(PlatformPermissions.ManageTenantAdministrators))
            {
                Assert.NotNull(dashboard.Data.InvitationsPending);
                Assert.NotNull(dashboard.Data.InvitationDeliveryRetries);
            }
            else
            {
                Assert.Null(dashboard.Data.InvitationsPending);
                Assert.Null(dashboard.Data.InvitationDeliveryRetries);
            }
        }
        var kycRead = staff && scenario != "ReservedRoleClaims" && permissions.Split(',').Contains(PlatformPermissions.ViewKyc);
        await ExpectAsync(client, $"/api/v1/control-plane/tenants/{pending}/kyc/documents", kycRead);
        await ExpectAsync(client, "/api/v1/control-plane/kyc/retention/policies", kycRead);
        using var tenantWrite = await client.PostAsJsonAsync("/api/v1/control-plane/tenants", new
        {
        });

        Assert.Equal(HttpStatusCode.Forbidden, tenantWrite.StatusCode);
        using var policyWrite = await client.PostAsJsonAsync("/api/v1/control-plane/kyc/retention/policies",
            new PublishKycRetentionPolicyRequest(Guid.Empty, "Validation probe", 0, 0, 0, 0, 0, 0, "Validation only; do not publish"));

        Assert.Equal(scenario == "RetentionOperator" ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.Forbidden, policyWrite.StatusCode);
        await ExpectAsync(client, "/api/v1/iam/admin/users", iamRead);
        using var roleResponse = await client.PostAsJsonAsync("/api/v1/iam/admin/roles",
            new CreateRoleRequest($"Matrix-created-{Guid.CreateVersion7():N}"));

        Assert.Equal(roleWrite ? HttpStatusCode.OK : HttpStatusCode.Forbidden, roleResponse.StatusCode);

        var navigation = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<MenuResponse>>>(
            "/api/v1/iam/admin/menus/navigation/Sidebar");

        Assert.True(navigation!.IsSuccess, navigation.Message);
        Assert.Equal(platform || kycRead, Flatten(navigation.Data!).Any(menu => menu.Url == "/control-panel"));
        using var approvalResponse = await client.PostAsJsonAsync(
            $"/api/v1/control-plane/tenants/{pending}/approve", new
            {
                EvidenceReference = "Synthetic matrix review"
            });

        Assert.Equal(approval ? HttpStatusCode.OK : HttpStatusCode.Forbidden, approvalResponse.StatusCode);
    }

    [Fact]
    public async Task AssignmentsMoveBetweenQueuesAndRetainHistoryAfterWorkflowResolution()
    {
        using var app = CreateHost();
        const string reviewerPermissions = "control_plane.view_kyc,control_plane.approve_kyc";
        var manager = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "TaskLead", true, reviewerPermissions + ",control_plane.manage_tasks");
        var reviewer = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "TaskReviewer", true, reviewerPermissions);
        using var lead = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, manager.Name, true));
        using var worker = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, reviewer.Name, true));
        var subject = await PendingTenantAsync(app);
        var initial = await TaskPageAsync(lead, "kyc-review", "unassigned");
        var task = Assert.Single(initial.Items, item => item.Task.SubjectId == subject);
        Assert.True(initial.CanAssignOthers);
        await PostAsync<bool>(lead, "/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, reviewer.UserId, task.Revision));
        Assert.DoesNotContain((await TaskPageAsync(lead, "kyc-review", "unassigned")).Items, item => item.Task.SubjectId == subject);
        task = Assert.Single((await TaskPageAsync(worker, "kyc-review", "mine")).Items);
        Assert.Equal(reviewer.UserId, task.AssigneeUserId);
        Assert.True(task.AssigneeEligible);
        var attention = await worker.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>("/api/v1/dashboard/attention");
        Assert.Contains(attention!.Data!, item => item.Key == "assigned-kyc-review" && item.Kind == DashboardAttentionKind.AssignedAction && item.Count == 1);
        await PostAsync<bool>(worker, "/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, null, task.Revision));
        Assert.Empty((await TaskPageAsync(worker, "kyc-review", "mine")).Items);
        task = Assert.Single((await TaskPageAsync(worker, "kyc-review", "unassigned")).Items, item => item.Task.SubjectId == subject);
        await PostAsync<bool>(worker, "/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, reviewer.UserId, task.Revision));

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var customer = await context.Tenants.FindAsync(subject);
            customer!.Approve(manager.UserId, scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow(), "reviewed-fixture-evidence");
            await context.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IWorkTaskService>().ReconcileAsync("kyc-review", null, CancellationToken.None);
        }
        Assert.Empty((await TaskPageAsync(worker, "kyc-review", "mine")).Items);
        var history = await worker.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardTaskHistoryResponse>>>(TaskUrl(task.Task, "history"));
        Assert.True(history!.IsSuccess, history.Message);
        Assert.Contains(history.Data!, item => item.Action == "Assigned");
        Assert.Contains(history.Data!, item => item.Action == "Unassigned");
        Assert.Contains(history.Data!, item => item.Action == "Claimed");
        Assert.Contains(history.Data!, item => item.Action == "Resolved" && item.ActorName == "System");
        using var stale = await lead.PostAsJsonAsync("/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, manager.UserId, task.Revision));
        Assert.Equal(HttpStatusCode.NotFound, stale.StatusCode);
    }

    [Fact]
    public async Task AssigneeEligibilityRejectsMissingPermissionsWrongWorkspaceAndUnavailableAccounts()
    {
        using var app = CreateHost();
        const string required = "control_plane.view_kyc,control_plane.approve_kyc";
        var manager = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "EligibilityLead", true, required + ",control_plane.manage_tasks");
        using var client = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, manager.Name, true));
        var subject = await PendingTenantAsync(app);
        var task = Assert.Single((await TaskPageAsync(client, "kyc-review", "unassigned")).Items, item => item.Task.SubjectId == subject);
        var foreign = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(foreign);

        foreach (var scenario in new[] { "MissingPermission", "NotStaff", "ReservedRoleClaims", "WrongWorkspace", "Inactive", "Deleted", "Unconfirmed", "SetupIncomplete", "NoMfa" })
        {
            var owner = scenario == "WrongWorkspace" ? foreign : Tenant.PlatformTenantId;
            var identity = await CreateIdentityAsync(app, owner, scenario, scenario != "NotStaff",
                scenario == "MissingPermission" ? PlatformPermissions.ViewKyc : required);

            await using (var scope = app.Services.CreateAsyncScope())
            {
                scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = owner;
                var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var account = (await users.FindByIdAsync(identity.UserId))!;
                account.TwoFactorEnabled = scenario != "NoMfa";

                if (scenario == "Inactive")
                {
                    account.IsActive = false;
                }

                if (scenario == "Deleted")
                {
                    account.IsDeleted = true;
                }

                if (scenario == "Unconfirmed")
                {
                    account.EmailConfirmed = false;
                }

                if (scenario == "SetupIncomplete")
                {
                    account.BeginAdministratorSetup(Guid.CreateVersion7());
                }

                Assert.True((await users.UpdateAsync(account)).Succeeded);
            }
            using var response = await client.PostAsJsonAsync("/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, identity.UserId, task.Revision));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var candidates = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardTaskAssigneeResponse>>>(TaskUrl(task.Task, "assignees") + $"&search={identity.Name}");
            Assert.DoesNotContain(candidates!.Data!, candidate => candidate.UserId == identity.UserId);
        }
        Assert.Contains((await TaskPageAsync(client, "kyc-review", "unassigned")).Items, item => item.Task.SubjectId == subject);
        await using var verification = app.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
        Assert.False(await db.TaskAssignments.AnyAsync(item => item.SubjectId == subject));
    }

    [Fact]
    public async Task AssignmentRechecksPermissionsAfterTheAssigneeWasListed()
    {
        using var app = CreateHost();
        const string required = "control_plane.view_kyc,control_plane.approve_kyc";
        var manager = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "RecheckLead", true, required + ",control_plane.manage_tasks");
        var target = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "RecheckTarget", true, required);
        using var client = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, manager.Name, true));
        await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, target.Name, true);
        var subject = await PendingTenantAsync(app);
        var task = Assert.Single((await TaskPageAsync(client, "kyc-review", "unassigned")).Items, item => item.Task.SubjectId == subject);
        var candidates = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardTaskAssigneeResponse>>>(TaskUrl(task.Task, "assignees") + $"&search={target.Name}");
        Assert.Contains(candidates!.Data!, account => account.UserId == target.UserId);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var account = (await users.FindByIdAsync(target.UserId))!;
            Assert.True((await users.RemoveClaimAsync(account, new Claim("permission", PlatformPermissions.ApproveKyc))).Succeeded);
        }
        using var rejected = await client.PostAsJsonAsync("/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, target.UserId, task.Revision));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        var after = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardTaskAssigneeResponse>>>(TaskUrl(task.Task, "assignees") + $"&search={target.Name}");
        Assert.DoesNotContain(after!.Data!, account => account.UserId == target.UserId);
    }

    [Fact]
    public async Task ConcurrentClaimsHaveOneWinnerAndOrdinaryOperatorsCannotStealAssignments()
    {
        using var app = CreateHost();
        const string required = "control_plane.view_kyc,control_plane.approve_kyc";
        var first = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "FirstClaimant", true, required);
        var second = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "SecondClaimant", true, required);
        using var a = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, first.Name, true));
        using var b = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, second.Name, true));
        var subject = await PendingTenantAsync(app);
        var task = Assert.Single((await TaskPageAsync(a, "kyc-review", "unassigned")).Items, item => item.Task.SubjectId == subject);
        var outcomes = await Task.WhenAll(
            a.PostAsJsonAsync("/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, first.UserId, task.Revision)),
            b.PostAsJsonAsync("/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, second.UserId, task.Revision)));

        try
        {
            Assert.Single(outcomes, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(outcomes, response => response.StatusCode == HttpStatusCode.Conflict);
        }
        finally
        {
            foreach (var response in outcomes)
            {
                response.Dispose();
            }
        }
        var aMine = await TaskPageAsync(a, "kyc-review", "mine");
        var winner = aMine.Items.SingleOrDefault(item => item.Task.SubjectId == subject);
        var loser = winner is null ? a : b;
        var loserId = winner is null ? first.UserId : second.UserId;
        winner ??= Assert.Single((await TaskPageAsync(b, "kyc-review", "mine")).Items, item => item.Task.SubjectId == subject);
        using var stolen = await loser.PostAsJsonAsync("/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, loserId, winner.Revision));
        Assert.Equal(HttpStatusCode.Forbidden, stolen.StatusCode);
        using var released = await loser.PostAsJsonAsync("/api/v1/dashboard/tasks/kyc-review/assignment", new AssignDashboardTaskRequest(task.Task, null, winner.Revision));
        Assert.Equal(HttpStatusCode.Forbidden, released.StatusCode);
        using var all = await loser.GetAsync("/api/v1/dashboard/tasks/kyc-review?queue=all");
        Assert.Equal(HttpStatusCode.Forbidden, all.StatusCode);
        using var candidates = await loser.GetAsync(TaskUrl(task.Task, "assignees"));
        Assert.Equal(HttpStatusCode.Forbidden, candidates.StatusCode);
    }

    [Fact]
    public async Task CustomerTaskAssignmentsAndHistoryStayInsideTheSignedWorkspace()
    {
        using var app = CreateHost();
        var owner = Guid.CreateVersion7();
        var foreign = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(owner);
        await factory.RegisterTenantAsync(foreign);
        var manager = await CreateIdentityAsync(app, owner, "CustomerTaskLead", false, "support.approve,tasks.assign");
        var reviewer = await CreateIdentityAsync(app, owner, "CustomerTaskReviewer", false, "support.approve");
        var outsider = await CreateIdentityAsync(app, foreign, "ForeignTaskReviewer", false, "support.approve");
        using var client = Bearer(app, await SignInWithAuthenticatorAsync(app, owner, manager.Name, false));
        using var target = Bearer(app, await SignInWithAuthenticatorAsync(app, owner, reviewer.Name, false));
        await SignInWithAuthenticatorAsync(app, foreign, outsider.Name, false);
        var ownGrant = new SupportGrant { WorkspaceId = owner, VendorId = "fixture", CreatedBy = "fixture" };
        var foreignGrant = new SupportGrant { WorkspaceId = foreign, VendorId = "fixture", CreatedBy = "fixture" };

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            context.SupportGrants.AddRange(ownGrant, foreignGrant);
            await context.SaveChangesAsync();
        }
        var tasks = await TaskPageAsync(client, "support-approval", "unassigned");
        var item = Assert.Single(tasks.Items);
        Assert.Equal(ownGrant.Id, item.Task.SubjectId);
        using var rejected = await client.PostAsJsonAsync("/api/v1/dashboard/tasks/support-approval/assignment", new AssignDashboardTaskRequest(item.Task, outsider.UserId, item.Revision));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        var foreignKey = new DashboardTaskKey("support-approval", foreignGrant.Id, foreignGrant.Id, foreignGrant.CreatedAt);
        using var hidden = await client.GetAsync(TaskUrl(foreignKey, "assignees"));
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using var history = await client.GetAsync(TaskUrl(foreignKey, "history"));
        Assert.Equal(HttpStatusCode.NotFound, history.StatusCode);
        await PostAsync<bool>(client, "/api/v1/dashboard/tasks/support-approval/assignment", new AssignDashboardTaskRequest(item.Task, reviewer.UserId, item.Revision));
        Assert.Empty((await TaskPageAsync(client, "support-approval", "unassigned")).Items);
        Assert.Single((await TaskPageAsync(target, "support-approval", "mine")).Items);
    }

    [Fact]
    public async Task ProvisioningAssignmentsDoNotCarryOverToAnotherAttempt()
    {
        using var app = CreateHost();
        var identity = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "ProvisioningTaskOperator", true,
            "control_plane.view_tenants,control_plane.manage_tenant_lifecycle");

        using var client = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, identity.Name, true));
        var subject = await PendingTenantAsync(app);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var customer = (await db.Tenants.FindAsync(subject))!;
            var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
            customer.Approve(identity.UserId, now, "attempt-fixture");
            customer.BeginProvisioning(now, TimeSpan.FromMinutes(10));
            customer.MarkAsProvisioningFailed();
            await db.SaveChangesAsync();
        }
        var task = Assert.Single((await TaskPageAsync(client, "provisioning-failed", "unassigned")).Items, item => item.Task.SubjectId == subject);
        await PostAsync<bool>(client, "/api/v1/dashboard/tasks/provisioning-failed/assignment", new AssignDashboardTaskRequest(task.Task, identity.UserId, task.Revision));

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var customer = (await db.Tenants.FindAsync(subject))!;
            var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
            customer.RetryProvisioning(now);
            customer.BeginProvisioning(now, TimeSpan.FromMinutes(10));
            customer.MarkAsProvisioningFailed();
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<IWorkTaskService>().ReconcileAsync("provisioning-failed", null, CancellationToken.None);
        }
        Assert.DoesNotContain((await TaskPageAsync(client, "provisioning-failed", "mine")).Items, item => item.Task.SubjectId == subject);
        var next = Assert.Single((await TaskPageAsync(client, "provisioning-failed", "unassigned")).Items, item => item.Task.SubjectId == subject);
        Assert.NotEqual(task.Task.CycleId, next.Task.CycleId);
        Assert.Null(next.Revision);
        using var stale = await client.PostAsJsonAsync("/api/v1/dashboard/tasks/provisioning-failed/assignment", new AssignDashboardTaskRequest(task.Task, identity.UserId, null));
        Assert.Equal(HttpStatusCode.NotFound, stale.StatusCode);
    }

    [Fact]
    public async Task TaskManagementCannotReplaceActionAuthorityAndTaskPagingIsStable()
    {
        using var app = CreateHost();
        var managerOnly = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "TaskManagementOnly", true, PlatformPermissions.ManageTasks);
        using var denied = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, managerOnly.Name, true));
        using var forbidden = await denied.GetAsync("/api/v1/dashboard/tasks/kyc-review");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var reviewer = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "PagedReviewer", true, "control_plane.view_kyc,control_plane.approve_kyc");
        using var client = Bearer(app, await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, reviewer.Name, true));
        await PendingTenantAsync(app);
        await PendingTenantAsync(app);
        var first = await client.GetFromJsonAsync<AppResponse<DashboardTaskPageResponse>>("/api/v1/dashboard/tasks/kyc-review?queue=unassigned&page=1&pageSize=1");
        var second = await client.GetFromJsonAsync<AppResponse<DashboardTaskPageResponse>>("/api/v1/dashboard/tasks/kyc-review?queue=unassigned&page=2&pageSize=1");
        Assert.True(first!.IsSuccess);
        Assert.True(second!.IsSuccess);
        Assert.Equal(first.Data!.Total, second.Data!.Total);
        Assert.NotEqual(Assert.Single(first.Data.Items).Task.SubjectId, Assert.Single(second.Data.Items).Task.SubjectId);
        var foreign = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(foreign);
        var foreignStaff = await CreateIdentityAsync(app, foreign, "ForeignStaffTask", true, "control_plane.view_kyc,control_plane.approve_kyc,control_plane.manage_tasks");
        using var wrongWorkspace = Bearer(app, await SignInWithAuthenticatorAsync(app, foreign, foreignStaff.Name, true));
        using var blocked = await wrongWorkspace.GetAsync("/api/v1/dashboard/tasks/kyc-review");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        var attention = await wrongWorkspace.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>("/api/v1/dashboard/attention");
        Assert.DoesNotContain(attention!.Data!, item => item.Key == "kyc-review");
    }

    private static async Task<DashboardTaskPageResponse> TaskPageAsync(HttpClient client, string kind, string queue)
    {
        var response = await client.GetFromJsonAsync<AppResponse<DashboardTaskPageResponse>>($"/api/v1/dashboard/tasks/{kind}?queue={queue}&pageSize=100");
        Assert.True(response!.IsSuccess, response.Message);
        return response.Data!;
    }

    private static string TaskUrl(DashboardTaskKey key, string operation)
    {
        return $"/api/v1/dashboard/tasks/{key.Kind}/{operation}?subjectId={key.SubjectId:D}&cycleId={key.CycleId:D}&cycleAt={Uri.EscapeDataString(key.CycleAt.ToString("O"))}";
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedProvisioningIsTeamWorkOnlyForLifecycleOperators(bool canManageLifecycle)
    {
        using var app = CreateHost();
        var permissions = PlatformPermissions.ViewTenants;

        if (canManageLifecycle)
        {
            permissions += $",{PlatformPermissions.ManageTenantLifecycle}";
        }
        var identity = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "LifecycleAttention", true, permissions);
        var workspace = await PendingTenantAsync(app);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var customer = await context.Tenants.FindAsync(workspace);
            var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
            customer!.Approve("fixture", now, "fixture-evidence");
            customer.BeginProvisioning(now, TimeSpan.FromMinutes(10));
            customer.MarkAsProvisioningFailed();
            await context.SaveChangesAsync();
        }
        var token = await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, identity.Name, true);
        using var client = Bearer(app, token);
        var response = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>("/api/v1/dashboard/attention");
        Assert.True(response!.IsSuccess, response.Message);
        Assert.Equal(canManageLifecycle, response.Data!.Any(item => item.Key == "provisioning-failed"));

        if (canManageLifecycle)
        {
            var item = Assert.Single(response.Data!, item => item.Key == "provisioning-failed");
            Assert.Equal(DashboardAttentionKind.TeamAction, item.Kind);
            Assert.Contains(PlatformPermissions.ManageTenantLifecycle, item.RequiredPermissions);
        }
        // Readers still receive authorized metrics, without being told to resolve failures.
        await ExpectAsync(client, "/api/v1/control-plane/tenants/dashboard", true);
    }

    [Fact]
    public async Task ApprovedSupportIsInformationAndOnlyIncludesTheCurrentActorsUnexpiredAccess()
    {
        using var app = CreateHost();
        var workspace = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(workspace);
        var identity = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "SupportInformation", true,
            $"{PlatformPermissions.ViewSupport},{PlatformPermissions.ManageSupport}");

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
            var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            context.Set<SupportGrant>().AddRange(
                new SupportGrant { WorkspaceId = workspace, VendorId = identity.UserId, Status = "Approved", ExpiresAt = now.AddHours(1), CreatedBy = "fixture" },
                new SupportGrant { WorkspaceId = workspace, VendorId = "another-operator", Status = "Approved", ExpiresAt = now.AddHours(1), CreatedBy = "fixture" },
                new SupportGrant { WorkspaceId = workspace, VendorId = identity.UserId, Status = "Approved", ExpiresAt = now.AddHours(-1), CreatedBy = "fixture" });

            await context.SaveChangesAsync();
        }
        var token = await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, identity.Name, true);
        using var client = Bearer(app, token);
        var response = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>("/api/v1/dashboard/attention");
        Assert.True(response!.IsSuccess, response.Message);
        var item = Assert.Single(response.Data!);
        Assert.Equal("support-ready", item.Key);
        Assert.Equal(1, item.Count);
        Assert.Equal(DashboardAttentionKind.Information, item.Kind);
        Assert.Equal("/control-panel/support", item.Href);
    }

    [Fact]
    public async Task AttentionSupportRequestsStayInTheSignedWorkspaceAndDisappearAfterRoleRevocation()
    {
        using var app = CreateHost();
        var workspaceA = Guid.CreateVersion7();
        var workspaceB = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(workspaceA);
        await factory.RegisterTenantAsync(workspaceB);
        var identity = await CreateIdentityAsync(app, workspaceA, "AttentionApprover", false, "support.approve");

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            context.Set<SupportGrant>().AddRange(
                new SupportGrant { WorkspaceId = workspaceA, VendorId = "fixture-vendor", CreatedBy = "fixture", Reason = "Workspace A request" },
                new SupportGrant { WorkspaceId = workspaceB, VendorId = "fixture-vendor", CreatedBy = "fixture", Reason = "Workspace B request must remain private" });

            await context.SaveChangesAsync();
        }
        var token = await SignInWithAuthenticatorAsync(app, workspaceA, identity.Name, false);
        using var client = Bearer(app, token);
        var before = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>("/api/v1/dashboard/attention");
        Assert.True(before!.IsSuccess, before.Message);
        var request = Assert.Single(before.Data!, item => item.Key == "support-approval");
        Assert.Equal(1, request.Count);
        Assert.Equal("/tasks?kind=support-approval&queue=unassigned", request.Href);
        Assert.Equal(DashboardAttentionKind.TeamAction, request.Kind);
        Assert.DoesNotContain("your approval", request.Title, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(request.Reason));
        Assert.False(string.IsNullOrWhiteSpace(request.ActionLabel));
        Assert.DoesNotContain(before.Data!, item => item.RequiredPermissions.Any(key => key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase)));

        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = workspaceA;
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var role = await roles.FindByIdAsync(identity.RoleId);
            Assert.True((await roles.RemoveClaimAsync(role!, new Claim("permission", "support.approve"))).Succeeded);
        }
        var after = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>("/api/v1/dashboard/attention");
        Assert.True(after!.IsSuccess, after.Message);
        Assert.DoesNotContain(after.Data!, item => item.Key == "support-approval");
    }

    [Fact]
    public async Task DashboardCountsCustomersWithoutIncludingDemoWorkspaces()
    {
        using var app = CreateHost();
        var identity = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "DashboardCounts", true, Manage);
        var token = await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, identity.Name, true);
        using var client = Bearer(app, token);
        var before = await client.GetFromJsonAsync<AppResponse<PlatformDashboardResponse>>(
            "/api/v1/control-plane/tenants/dashboard");

        await PendingTenantAsync(app);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var id = Guid.CreateVersion7();
            context.Tenants.Add(new Tenant
            {
                Id = id,
                Identifier = $"demo-{id:N}",
                DisplayName = "Synthetic demo must not count as a customer",
                HostName = $"{id:N}.invalid",
                IsDemo = true,
                DeploymentStampId = Tenant.PlatformTenantId,
                CreatedBy = "fixture",
                MaxUsers = 10
            });

            await context.SaveChangesAsync();
        }
        var after = await client.GetFromJsonAsync<AppResponse<PlatformDashboardResponse>>(
            "/api/v1/control-plane/tenants/dashboard");

        Assert.True(before!.IsSuccess);
        Assert.True(after!.IsSuccess);
        Assert.Equal(before.Data!.CustomerTenants + 1, after.Data!.CustomerTenants);
        Assert.Equal(before.Data.AwaitingKyc + 1, after.Data.AwaitingKyc);
        Assert.Equal(before.Data.ActiveTenants, after.Data.ActiveTenants);
        Assert.Null(after.Data.InvitationsPending);
    }

    [Fact]
    public async Task ExistingPlatformSessionLosesDirectPermissionsAndStaffAuthorityWithoutRefresh()
    {
        using var app = CreateHost();
        var identity = await CreateIdentityAsync(app, Tenant.PlatformTenantId, "Revocation", true, $"{Manage},{Approve}");
        var pending = await PendingTenantAsync(app);
        var token = await SignInWithAuthenticatorAsync(app, Tenant.PlatformTenantId, identity.Name, true);
        using var client = Bearer(app, token);
        await ExpectAsync(client, $"/api/v1/control-plane/tenants/{pending}/kyc/documents", true);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByIdAsync(identity.UserId);
            Assert.True((await users.RemoveClaimAsync(user!, new Claim("permission", Approve))).Succeeded);
        }
        await ExpectAsync(client, $"/api/v1/control-plane/tenants/{pending}/kyc/documents", false);
        await ExpectAsync(client, "/api/v1/control-plane/tenants", true);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByIdAsync(identity.UserId);
            Assert.True((await users.RemoveClaimAsync(user!, new Claim("platform_staff", "true"))).Succeeded);
        }
        await ExpectAsync(client, "/api/v1/control-plane/tenants", false);
        await ExpectAsync(client, "/api/v1/control-plane/tenants/dashboard", false);
        var navigation = await client.GetFromJsonAsync<AppResponse<IReadOnlyList<MenuResponse>>>(
            "/api/v1/iam/admin/menus/navigation/Sidebar");

        Assert.DoesNotContain(Flatten(navigation!.Data!), menu => menu.Url == "/control-panel");
        using var logout = await client.PostAsync("/api/v1/iam/auth/logout", null);
        logout.EnsureSuccessStatusCode();
        using var revoked = await client.GetAsync("/api/v1/iam/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    [Fact]
    public async Task CustomerAdministratorCannotUseReservedPermissionsToEnterThePlatform()
    {
        using var app = CreateHost();
        var tenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        var identity = await CreateIdentityAsync(app, tenant, "AdministratorLabel", false,
            $"{Manage},{Approve},users.view");

        var token = await SignInWithAuthenticatorAsync(app, tenant, identity.Name, false);
        using var client = Bearer(app, token);
        await ExpectAsync(client, "/api/v1/iam/admin/users", true);
        await ExpectAsync(client, "/api/v1/control-plane/tenants", false);
        await ExpectAsync(client, "/api/v1/control-plane/tenants/dashboard", false);
    }

    private WebApplicationFactory<Program> CreateHost()
    {
        return factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:LoginPolicy:PermitLimit"] = "1000",
                ["RateLimiting:TwoFactorPolicy:PermitLimit"] = "1000"
            })));
    }

    private static async Task<(string UserId, string Name, string RoleId)> CreateIdentityAsync(
        WebApplicationFactory<Program> app, Guid tenant, string scenario, bool staff, string permissions)
    {
        await using var scope = app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenant;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var name = $"matrix-{Guid.CreateVersion7():N}";
        var roleName = scenario == "AdministratorLabel" ? "System Administrator" : $"{scenario}-{Guid.CreateVersion7():N}";
        var role = new AppRole { Name = roleName, TenantId = tenant };
        Assert.True((await roles.CreateAsync(role)).Succeeded);

        foreach (var permission in permissions.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!permission.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) || scenario == "ReservedRoleClaims")
            {
                Assert.True((await roles.AddClaimAsync(role, new Claim("permission", permission))).Succeeded);
            }
        }
        var user = AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, tenant, name, $"{name}@example.test", "fixture");
        user.IsActive = true;
        user.EmailConfirmed = true;
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, roleName)).Succeeded);
        // Reserved platform grants are explicitly assigned; ordinary tenant roles cannot confer them.
        foreach (var permission in permissions.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Where(key => key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) && scenario != "ReservedRoleClaims"))
        {
            Assert.True((await users.AddClaimAsync(user, new Claim("permission", permission))).Succeeded);
        }

        if (staff)
        {
            Assert.True((await users.AddClaimAsync(user, new Claim("platform_staff", "true"))).Succeeded);
        }
        return (user.Id, name, role.Id);
    }

    private static async Task<string> SignInWithAuthenticatorAsync(
        WebApplicationFactory<Program> app, Guid tenant, string name, bool platform, Action<string>? captureEnrollmentKey = null)
    {
        using var anonymous = app.CreateClient(new()
        {
            HandleCookies = false
        });

        anonymous.DefaultRequestHeaders.Add("X-Tenant-Id", tenant.ToString());
        var credentials = new
        {
            UserName = name,
            Password,
            RememberMe = false,
            DeviceFingerprint = "platform-matrix"
        };

        var initial = await PostAsync<LoginResponse>(anonymous, "/api/v1/iam/auth/login", credentials);
        using var setup = Bearer(app, initial.Token);

        if (platform)
        {
            Assert.True(initial.MfaEnrollmentRequired);
            var profile = await setup.GetFromJsonAsync<AppResponse<CurrentUserResponse>>("/api/v1/iam/auth/me");
            Assert.True(profile!.Data!.MfaEnrollmentRequired);
            Assert.False(profile.Data.HasMfaProof);
            using var attentionResponse = await setup.GetAsync("/api/v1/dashboard/attention");

            if (attentionResponse.IsSuccessStatusCode)
            {
                var attention = await attentionResponse.Content.ReadFromJsonAsync<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>();
                Assert.Empty(attention!.Data!);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Forbidden, attentionResponse.StatusCode);
            }
            await ExpectAsync(setup, "/api/v1/control-plane/tenants", false);
        }
        var enrollment = await PostAsync<TwoFactorSetupInfo>(setup, "/api/v1/iam/users/totp/setup", null);
        captureEnrollmentKey?.Invoke(enrollment.ManualEntryKey);
        var otp = new Totp(Base32Encoding.ToBytes(enrollment.ManualEntryKey));
        await PostAsync<VerifyOtpResponse>(setup, "/api/v1/iam/users/totp/verify",
            new VerifyOtpRequest { UserId = initial.UserId, Code = otp.ComputeTotp(), DeviceFingerprint = "platform-matrix" });

        var challenge = await PostAsync<LoginResponse>(anonymous, "/api/v1/iam/auth/login", credentials);
        Assert.True(challenge.Requires2FA);
        using var firstFactor = Bearer(app, challenge.Token);
        using var blocked = await firstFactor.GetAsync("/api/v1/control-plane/tenants");
        Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);
        using var blockedAttention = await firstFactor.GetAsync("/api/v1/dashboard/attention");
        Assert.Equal(HttpStatusCode.Unauthorized, blockedAttention.StatusCode);
        var verified = await PostAsync<VerifyOtpResponse>(anonymous, "/api/v1/iam/users/totp/verify",
            new VerifyOtpRequest
            {
                UserId = initial.UserId,
                Code = otp.ComputeTotp(),
                ChallengeToken = challenge.Token,
                DeviceFingerprint = "platform-matrix"
            });

        Assert.True(verified.IsAuthenticated);
        return verified.Token;
    }

    private static async Task<Guid> PendingTenantAsync(WebApplicationFactory<Program> app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
        var id = Guid.CreateVersion7();
        context.Tenants.Add(new Tenant
        {
            Id = id,
            Identifier = $"matrix-{id:N}",
            DisplayName = "Synthetic role matrix",
            HostName = $"{id:N}.invalid",
            DeploymentStampId = Tenant.PlatformTenantId,
            CreatedBy = "fixture",
            MaxUsers = 10
        });

        await context.SaveChangesAsync();
        return id;
    }

    private static HttpClient Bearer(WebApplicationFactory<Program> app, string token)
    {
        var client = app.CreateClient(new()
        {
            HandleCookies = false
        });

        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private static async Task ExpectAsync(HttpClient client, string path, bool allowed)
    {
        using var response = await client.GetAsync(path);
        Assert.True(response.StatusCode == (allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden),
            $"Expected {(allowed ? 200 : 403)}, got {(int)response.StatusCode}: {path}");
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string path, object? body)
    {
        using var response = body is null ? await client.PostAsync(path, null) : await client.PostAsJsonAsync(path, body);
        Assert.True(response.IsSuccessStatusCode, $"Matrix sign-in request returned HTTP {(int)response.StatusCode}: {path}");
        var result = await response.Content.ReadFromJsonAsync<AppResponse<T>>();
        Assert.True(result!.IsSuccess, result.Message);
        return result.Data!;
    }

    private static IEnumerable<MenuResponse> Flatten(IEnumerable<MenuResponse> menus)
    {
        foreach (var menu in menus)
        {
            yield return menu;

            foreach (var child in Flatten(menu.Children))
            {
                yield return child;
            }
        }
    }
}
