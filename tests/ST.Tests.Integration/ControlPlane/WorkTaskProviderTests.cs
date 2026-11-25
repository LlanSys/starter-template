using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NSubstitute;
using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Persistence.Features.ControlPlane.Tenants.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Support.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Features.Shared.WorkTasks;
using ST.Persistence.Features.ControlPlane;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.Shared.Dashboard;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

public abstract class WorkTaskProviderTests<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : DbFixture
{
    [Fact]
    public async Task MigrationsSourceKeysAssignmentsAndHistoryWorkOnTheProvider()
    {
        var database = $"TaskAssignments_{Guid.CreateVersion7():N}";
        var connection = TestDatabaseProvider.WithDatabase(fixture, database);

        var tenant = new TaskTenant();
        var actor = new TaskActor();
        var clock = TimeProvider.System;
        ControlPlaneDBContext Plane()
        {
            // <db:PostgreSql>
            if (fixture is PostgreSqlDbFixture)
            {
                return new ControlPlanePostgreSqlDBContext(new DbContextOptionsBuilder<ControlPlanePostgreSqlDBContext>().UseNpgsql(connection, options => options.MigrationsHistoryTable("__EFMigrationsHistory_ControlPlane")).Options, clock, actor);
            }
            // </db:PostgreSql>
            // <db:SqlServer>
            if (fixture is MsSqlDbFixture)
            {
                return new ControlPlaneSqlServerDBContext(new DbContextOptionsBuilder<ControlPlaneSqlServerDBContext>().UseSqlServer(connection, options => options.MigrationsHistoryTable("__EFMigrationsHistory_ControlPlane")).Options, clock, actor);
            }
            // </db:SqlServer>
            throw TestDatabaseProvider.Unsupported(fixture);
        }
        await using var plane = Plane();
        await plane.Database.MigrateAsync();
        IamDBContext? iamCreated = null;
        // <db:PostgreSql>
        if (fixture is PostgreSqlDbFixture)
        {
            iamCreated = new IamPostgreSqlDBContext(new DbContextOptionsBuilder<IamPostgreSqlDBContext>().UseNpgsql(connection, options => options.MigrationsHistoryTable("__EFMigrationsHistory_IAM")).Options, clock, tenant, actor);
        }
        // </db:PostgreSql>
        // <db:SqlServer>
        if (fixture is MsSqlDbFixture)
        {
            iamCreated = new IamSqlServerDBContext(new DbContextOptionsBuilder<IamSqlServerDBContext>().UseSqlServer(connection, options => options.MigrationsHistoryTable("__EFMigrationsHistory_IAM")).Options, clock, tenant, actor);
        }
        // </db:SqlServer>
        await using IamDBContext iam = iamCreated ?? throw TestDatabaseProvider.Unsupported(fixture);

        var baseline = iam.Database.GetMigrations().Single(id => id.EndsWith("_AddTaskAssignmentPermissions", StringComparison.Ordinal));
        await iam.GetService<IMigrator>().MigrateAsync(baseline);
        var managed = new AppRole { Id = Guid.CreateVersion7().ToString(), TenantId = Guid.CreateVersion7(), Name = "Tenant Administrator", NormalizedName = "TENANT ADMINISTRATOR", TemplateKey = "iam-administrator" };
        var custom = new AppRole { Id = Guid.CreateVersion7().ToString(), TenantId = Guid.CreateVersion7(), Name = "Custom Reviewers", NormalizedName = "CUSTOM REVIEWERS", TemplateKey = "iam-administrator" };
        var labelOnly = new AppRole { Id = Guid.CreateVersion7().ToString(), TenantId = Guid.CreateVersion7(), Name = "Tenant Administrator", NormalizedName = "TENANT ADMINISTRATOR" };
        var deleted = new AppRole { Id = Guid.CreateVersion7().ToString(), TenantId = Guid.CreateVersion7(), Name = "Tenant Administrator", NormalizedName = "TENANT ADMINISTRATOR", TemplateKey = "iam-administrator", IsDeleted = true };
        var roleCases = new[] { managed, custom, labelOnly, deleted };

        foreach (var role in roleCases)
        {
            tenant.TenantId = role.TenantId;
            iam.Roles.Add(role);
            await iam.SaveChangesAsync();
        }
        tenant.TenantId = Tenant.PlatformTenantId;
        await iam.Database.MigrateAsync();
        var roleIds = roleCases.Select(role => role.Id).ToArray();
        var upgraded = await iam.RoleClaims.Where(claim => roleIds.Contains(claim.RoleId) && claim.ClaimType == "permission" && claim.ClaimValue == "tasks.assign")
            .Select(claim => claim.RoleId).ToArrayAsync();

        Assert.Equal(managed.Id, Assert.Single(upgraded));
        var now = clock.GetUtcNow();
        var customer = Customer("Pending review");
        var failed = Customer("Failed provisioning");
        failed.Approve(actor.ActorId, now, "provider-evidence");
        failed.BeginProvisioning(now, TimeSpan.FromMinutes(10));
        failed.MarkAsProvisioningFailed();
        var active = Customer("Active customer");
        active.Approve(actor.ActorId, now, "provider-evidence");
        active.MarkAsActive();
        plane.Tenants.AddRange(customer, failed, active);
        await plane.SaveChangesAsync();
        var invitation = new TenantAdministratorInvitation(active.Id, "fixture@example.test", "Test", "Admin", actor.ActorId, now);
        Assert.True(invitation.TryIssue(new string('A', 64), now, TimeSpan.FromHours(1)));
        invitation.ReportDelivery(false, now);
        plane.AdministratorInvitations.Add(invitation);
        var policy = await plane.KycRetentionPolicies.FirstAsync();
        var hold = new KycRetentionCase(customer.Id, policy.Id, now.AddYears(-2));
        hold.SetHold(true, now.AddYears(-2), policy);
        plane.KycRetentionCases.Add(hold);
        plane.SupportGrants.Add(new SupportGrant { WorkspaceId = active.Id, VendorId = actor.ActorId, Reason = "Provider test", CreatedBy = actor.ActorId });
        await plane.SaveChangesAsync();
        var account = AppUser.CreateSystemUser(now, tenant.TenantId, "provider-reviewer", "provider@example.test", actor.ActorId);
        account.Id = actor.ActorId;
        account.IsActive = true;
        account.EmailConfirmed = true;
        account.TwoFactorEnabled = true;
        iam.Users.Add(account);
        iam.UserClaims.AddRange(new IdentityUserClaim<string> { UserId = account.Id, ClaimType = PlatformAccess.StaffClaim, ClaimValue = "true" },
            new IdentityUserClaim<string> { UserId = account.Id, ClaimType = "permission", ClaimValue = PlatformPermissions.ViewKyc },
            new IdentityUserClaim<string> { UserId = account.Id, ClaimType = "permission", ClaimValue = PlatformPermissions.ApproveKyc });

        await iam.SaveChangesAsync();
        using var unit = new ControlPlaneUnitOfWork(plane, new TenantRepository(plane), new DeploymentStampRepository(plane), Substitute.For<IImpersonationRecordRepository>());
        var sources = new WorkTaskSourceQuery(unit);

        foreach (var kind in DashboardTaskCatalog.Definitions.Keys)
        {
            var source = await sources.ForKind(kind, now, null).Take(10).ToArrayAsync();
            Assert.NotEmpty(source);
            Assert.All(source, item => Assert.NotEqual(Guid.Empty, item.SubjectId));
        }
        var service = new WorkTaskService(unit, sources, new TaskAssigneeEligibility(iam, tenant, new TaskPolicy()),
            tenant, actor, iam, clock, NullLogger<WorkTaskService>.Instance);

        var page = await service.ListAsync("kyc-review", "unassigned", 1, 100, false, CancellationToken.None);
        Assert.True(page.IsSuccess, page.Message);
        var task = Assert.Single(page.Data!.Items, item => item.Task.SubjectId == customer.Id);
        var assigned = await service.AssignAsync(new AssignDashboardTaskRequest(task.Task, actor.ActorId, task.Revision), false, CancellationToken.None);
        Assert.True(assigned.IsSuccess, assigned.Message);
        Assert.Equal(1, (await service.CountsAsync("kyc-review", CancellationToken.None)).Mine);
        var record = await plane.TaskAssignments.SingleAsync(item => item.SubjectId == customer.Id);

        await using (var contender = Plane())
        {
            var stale = await contender.TaskAssignments.SingleAsync(item => item.Id == record.Id);
            record.Assign(null);
            await plane.SaveChangesAsync();
            stale.Assign("stale-contender");
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contender.SaveChangesAsync());
        }
        record.Complete(now);
        await plane.SaveChangesAsync();
        var reopened = await service.ListAsync("kyc-review", "unassigned", 1, 100, false, CancellationToken.None);
        var openTask = Assert.Single(reopened.Data!.Items, item => item.Task.SubjectId == customer.Id);
        Assert.Null(openTask.Revision);
        Assert.True((await service.AssignAsync(new AssignDashboardTaskRequest(openTask.Task, actor.ActorId, null), false, CancellationToken.None)).IsSuccess);
        customer.Approve(actor.ActorId, now, "provider-completion");
        await plane.SaveChangesAsync();
        await service.ReconcileAsync("kyc-review", null, CancellationToken.None);
        Assert.Null(await service.ReconcileAsync("kyc-review", record.Id, CancellationToken.None));
        var history = await service.HistoryAsync(task.Task, CancellationToken.None);
        Assert.Contains(history.Data!, item => item.Action == "Reopened");
        Assert.Contains(history.Data!, item => item.Action == "Resolved");
        Assert.Equal(0, (await service.CountsAsync("kyc-review", CancellationToken.None)).Mine);
        Assert.False(plane.Database.HasPendingModelChanges());
        Assert.False(iam.Database.HasPendingModelChanges());
    }

    private static Tenant Customer(string name)
    {
        var id = Guid.CreateVersion7();
        return new Tenant
        {
            Id = id,
            Identifier = $"provider-{id:N}",
            DisplayName = name,
            HostName = $"{id:N}.invalid",
            DeploymentStampId = Tenant.PlatformTenantId,
            CreatedBy = "provider-fixture",
            MaxUsers = 10
        };
    }

    private sealed class TaskTenant : ICurrentTenantProvider
    {
        public Guid TenantId { get; set; } = Tenant.PlatformTenantId;
    }

    private sealed class TaskActor : ICurrentActorProvider
    {
        public string ActorId { get; } = Guid.CreateVersion7().ToString();
    }

    private sealed class TaskPolicy : IEffectiveModulePolicy
    {
        public Task<EffectiveModuleAccess> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EffectiveModuleAccess { Modules = new HashSet<string> { "core", "iam" }, Catalogue = [], EnabledFlags = new HashSet<string>() });
        }
    }
}
