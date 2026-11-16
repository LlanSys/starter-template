using Microsoft.AspNetCore.Identity;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Features.ControlPlane;
using MassTransit.EntityFrameworkCoreIntegration;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Npgsql;
using Quartz;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Common;
using System.Text.RegularExpressions;

namespace ST.Tests.Integration;

public abstract class FreshStarterMigrationTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture : DbFixture
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllRetainedContextsMigrateAndRerunIdempotently(bool upgrade)
    {
        bool postgres = fixture is PostgreSqlDbFixture;
        string connection = fixture.GetConnectionString();
        var database = upgrade ? "StarterUpgrade" : "StarterCertification";

        if (postgres)
        {
            connection = new NpgsqlConnectionStringBuilder(connection) { Database = database }.ConnectionString;
        }
        else
        {
            var builder = new SqlConnectionStringBuilder(connection) { InitialCatalog = database };
            connection = builder.ConnectionString;
        }
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICurrentTenantProvider, MigrationTenantProvider>();
        services.AddSingleton<ICurrentActorProvider, MigrationActorProvider>();
        void Configure(DbContextOptionsBuilder options, string context)
        {
            if (postgres)
            {
                options.UseNpgsql(connection, provider => provider.MigrationsHistoryTable("__EFMigrationsHistory_" + context));
            }
            else
            {
                options.UseSqlServer(connection, provider => provider.MigrationsHistoryTable("__EFMigrationsHistory_" + context));
            }
        }

        if (postgres)
        {
            services.AddDbContext<ControlPlanePostgreSqlDBContext>(options => Configure(options, "ControlPlane"));
            services.AddDbContext<IamPostgreSqlDBContext>(options => Configure(options, "IAM"));
            services.AddDbContext<SharedPostgreSqlDBContext>(options => Configure(options, "Shared"));
        }
        else
        {
            services.AddDbContext<ControlPlaneSqlServerDBContext>(options => Configure(options, "ControlPlane"));
            services.AddDbContext<IamSqlServerDBContext>(options => Configure(options, "IAM"));
            services.AddDbContext<SharedSqlServerDBContext>(options => Configure(options, "Shared"));
        }
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Type[] contexts = postgres
            ? [typeof(ControlPlanePostgreSqlDBContext), typeof(IamPostgreSqlDBContext), typeof(SharedPostgreSqlDBContext)]
            : [typeof(ControlPlaneSqlServerDBContext), typeof(IamSqlServerDBContext), typeof(SharedSqlServerDBContext)];

        var legacyKycId = Guid.CreateVersion7();
        var outboxId = Guid.CreateVersion7();
        var schedulerName = $"StarterMigration-{Guid.CreateVersion7():N}";
        var legacyFireTime = TimeProvider.System.GetUtcNow().AddDays(1);

        foreach (var type in contexts)
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(type);
            Assert.False(context.Database.HasPendingModelChanges());
            var previous = context.Database.GetMigrations().LastOrDefault(id => id.EndsWith("_WorkspaceOnboardingAndSupport", StringComparison.Ordinal)
                || (context is SharedDBContext && id.EndsWith("_InitialStarter", StringComparison.Ordinal)));

            string[] legacyUsers = [];

            if (upgrade && previous is not null)
            {
                await context.GetService<IMigrator>().MigrateAsync(previous);

                if (context is ControlPlaneDBContext oldControl)
                {
                    var evidenceBaseline = context.Database.GetMigrations().TakeWhile(id => !id.EndsWith("_ClassifyKycDocuments", StringComparison.Ordinal)).Last();
                    await context.GetService<IMigrator>().MigrateAsync(evidenceBaseline);
                    // Project only historical columns; today's tenant model also contains columns introduced later.
                    var customerId = await oldControl.Tenants.AsNoTracking().Where(tenant => tenant.Identifier == "default")
                        .Select(tenant => tenant.Id).SingleAsync();

                    await context.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                        INSERT INTO "KycDocuments" ("Id", "TenantId", "DocumentType", "FileName", "ContentType", "Length", "Sha256", "UploadedBy", "UploadedAt", "Status", "Revision")
                        VALUES ({legacyKycId}, {customerId}, {"Historical permit"}, {"legacy.pdf"}, {"application/pdf"}, {24L}, {new string('A', 64)}, {"MigrationCertification"}, {DateTimeOffset.UnixEpoch}, {"Awaiting review"}, {Guid.CreateVersion7()})
                        """);

                    await context.Database.ExecuteSqlRawAsync("UPDATE \"Tenants\" SET \"DisplayName\" = 'Existing customer record' WHERE \"Identifier\" = 'default'");
                }

                if (context is IamDBContext iam)
                {
                    // Current identity entities need the complete pre-permission-upgrade schema.
                    var grantBaseline = context.Database.GetMigrations().TakeWhile(id => !id.EndsWith("_GranularPlatformPermissions", StringComparison.Ordinal)).Last();
                    await context.GetService<IMigrator>().MigrateAsync(grantBaseline);
                    ((MigrationTenantProvider)scope.ServiceProvider.GetRequiredService<ICurrentTenantProvider>()).TenantId = Tenant.PlatformTenantId;
                    var prepared = new List<string>();

                    for (var scenario = 0; scenario < 3; scenario++)
                    {
                        var name = $"upgrade-platform-{Guid.CreateVersion7():N}";
                        var account = AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, Tenant.PlatformTenantId, name, $"{name}@example.test", "MigrationCertification");
                        account.IsActive = true;
                        await SeedLegacyAccountAsync(iam, account, postgres);
                        prepared.Add(account.Id);
                        iam.UserClaims.Add(new IdentityUserClaim<string> { UserId = account.Id, ClaimType = "permission", ClaimValue = "control_plane.manage" });

                        if (scenario < 2)
                        {
                            iam.UserClaims.Add(new IdentityUserClaim<string> { UserId = account.Id, ClaimType = "platform_staff", ClaimValue = "true" });
                        }

                        if (scenario == 0)
                        {
                            iam.UserClaims.Add(new IdentityUserClaim<string> { UserId = account.Id, ClaimType = "permission", ClaimValue = "control_plane.approve_kyc" });
                        }

                        await iam.SaveChangesAsync();
                    }
                    legacyUsers = prepared.ToArray();
                }

                if (context is SharedDBContext)
                {
                    await SeedQuartzTriggerAsync(context, schedulerName, legacyFireTime);
                    await context.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO \"OutboxState\" (\"OutboxId\", \"LockId\", \"Created\", \"BusName\") VALUES ({outboxId}, {outboxId}, {DateTime.UtcNow}, {"legacy"})");
                }
            }

            await context.Database.MigrateAsync();

            if (legacyUsers.Length > 0 && context is IamDBContext migratedIam)
            {
                var grants = await migratedIam.UserClaims.Where(x => legacyUsers.Contains(x.UserId) && x.ClaimType == "permission").ToArrayAsync();
                Assert.DoesNotContain(grants, x => x.ClaimValue == PlatformPermissions.LegacyManage);
                Assert.Contains(grants, x => x.UserId == legacyUsers[0] && x.ClaimValue == PlatformPermissions.ManageKycRetention);
                Assert.Contains(grants, x => x.UserId == legacyUsers[1] && x.ClaimValue == PlatformPermissions.ViewTenants);
                Assert.DoesNotContain(grants, x => x.UserId == legacyUsers[1] && x.ClaimValue == PlatformPermissions.ApproveKyc);
                Assert.DoesNotContain(grants, x => x.UserId == legacyUsers[2]);
            }

            if (upgrade && context is ControlPlaneDBContext control)
            {
                var retained = await control.Tenants.SingleAsync(tenant => tenant.Identifier == "default");
                Assert.Equal("Existing customer record", retained.DisplayName);
                Assert.Null(retained.KycApprovedAt);
                var oldDocument = await control.KycDocuments.SingleAsync(document => document.Id == legacyKycId);
                Assert.Equal("legacy", oldDocument.DocumentTypeKey);
                Assert.Equal("Historical permit", oldDocument.DocumentType);
                Assert.Equal("legacy.pdf", oldDocument.FileName);
                Assert.Equal("Pending", oldDocument.SafetyStatus.ToString());
                Assert.Equal(DateTimeOffset.UnixEpoch, oldDocument.NextSafetyScanAt);
            }

            if (upgrade && context is SharedDBContext)
            {
                Assert.True(await context.Set<OutboxState>().AnyAsync(state => state.OutboxId == outboxId));
                var busNames = await context.Database.SqlQuery<string>(
                    $"SELECT \"BusName\" AS \"Value\" FROM \"OutboxState\" WHERE \"OutboxId\" = {outboxId}").ToListAsync();

                Assert.Equal("legacy", Assert.Single(busNames));
            }

            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            await context.Database.MigrateAsync();
            string script = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            Assert.Contains("InitialStarter", script, StringComparison.Ordinal);
            var batches = postgres ? new[] { script } : Regex.Split(script, @"^GO\s*$", RegexOptions.Multiline);

            foreach (var batch in batches.Where(batch => !string.IsNullOrWhiteSpace(batch)))
            {
                await context.Database.ExecuteSqlRawAsync(batch);
            }

            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
        }
        await VerifyQuartzAsync(connection, postgres, schedulerName, upgrade, legacyFireTime);
        await VerifyConcurrencyAsync(provider, contexts[0]);
    }
    private static async Task SeedLegacyAccountAsync(IamDBContext context, AppUser account, bool postgres)
    {
        // Exercise the historical schema, without asking today's model to insert new columns.
        if (postgres)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Users" ("Id", "TenantId", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                    "FirstName", "LastName", "FailedLoginAttempts", "RequirePasswordChange", "IsActive", "CreatedAt", "CreatedBy",
                    "IsDeleted", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount", "RowVersion")
                VALUES ({account.Id}, {account.TenantId}, {account.UserName}, {account.UserName!.ToUpperInvariant()}, {account.Email}, {account.Email!.ToUpperInvariant()},
                    {account.FirstName}, {account.LastName}, {0}, {false}, {true}, {account.CreatedAt}, {account.CreatedBy},
                    {false}, {false}, {false}, {false}, {false}, {0}, {new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }})
                """);
        }
        else
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Users" ("Id", "TenantId", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                    "FirstName", "LastName", "FailedLoginAttempts", "RequirePasswordChange", "IsActive", "CreatedAt", "CreatedBy",
                    "IsDeleted", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
                VALUES ({account.Id}, {account.TenantId}, {account.UserName}, {account.UserName!.ToUpperInvariant()}, {account.Email}, {account.Email!.ToUpperInvariant()},
                    {account.FirstName}, {account.LastName}, {0}, {false}, {true}, {account.CreatedAt}, {account.CreatedBy},
                    {false}, {false}, {false}, {false}, {false}, {0})
                """);
        }
    }

    private static async Task SeedQuartzTriggerAsync(DbContext context, string schedulerName, DateTimeOffset fireTime)
    {
        var jobType = typeof(QuartzMigrationProbeJob).AssemblyQualifiedName;
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO QRTZ_JOB_DETAILS
                (SCHED_NAME, JOB_NAME, JOB_GROUP, JOB_CLASS_NAME, IS_DURABLE, IS_NONCONCURRENT, IS_UPDATE_DATA, REQUESTS_RECOVERY)
            VALUES ({schedulerName}, 'legacy-job', 'DEFAULT', {jobType}, {true}, {false}, {false}, {false});
            INSERT INTO QRTZ_TRIGGERS
                (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP, JOB_NAME, JOB_GROUP, NEXT_FIRE_TIME, PRIORITY, TRIGGER_STATE, TRIGGER_TYPE, START_TIME, MISFIRE_INSTR)
            VALUES ({schedulerName}, 'legacy-trigger', 'DEFAULT', 'legacy-job', 'DEFAULT', {fireTime.UtcTicks}, 5, 'WAITING', 'SIMPLE', {fireTime.UtcTicks}, 0);
            INSERT INTO QRTZ_SIMPLE_TRIGGERS
                (SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP, REPEAT_COUNT, REPEAT_INTERVAL, TIMES_TRIGGERED)
            VALUES ({schedulerName}, 'legacy-trigger', 'DEFAULT', 0, 0, 0);
            """);
    }

    private static async Task VerifyQuartzAsync(
        string connection, bool postgres, string schedulerName, bool upgrade, DateTimeOffset legacyFireTime)
    {
        var completed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(completed);
        services.AddQuartz(options =>
        {
            options.ConfigureScheduler(settings => settings.InstanceName = schedulerName);
            options.UsePersistentStore(store =>
            {
                store.ConfigureStore(settings => settings.StoreJobDataAsStrings = true);
                store.UseNewtonsoftJsonSerializer();

                if (postgres)
                {
                    store.UsePostgres(connection);
                }
                else
                {
                    store.UseSqlServer(connection);
                }
            });
        });

        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();

        try
        {
            if (upgrade)
            {
                var retained = await scheduler.GetTrigger(new TriggerKey("legacy-trigger"));
                Assert.NotNull(retained);
                Assert.Equal(legacyFireTime, retained.NextFireTimeUtc);
            }

            var job = JobBuilder.Create<QuartzMigrationProbeJob>().UsingJobData("marker", "verified").Build();
            var trigger = TriggerBuilder.Create().StartNow().Build();
            await scheduler.ScheduleJob(job, trigger);
            await scheduler.Start();

            Assert.Equal("verified", await completed.Task.WaitAsync(TimeSpan.FromSeconds(20)));
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true);
        }
    }

    private static async Task VerifyConcurrencyAsync(ServiceProvider provider, Type contextType)
    {
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = (DbContext)firstScope.ServiceProvider.GetRequiredService(contextType);
        var second = (DbContext)secondScope.ServiceProvider.GetRequiredService(contextType);
        var original = await first.Set<Tenant>().FirstAsync();
        var stale = await second.Set<Tenant>().SingleAsync(tenant => tenant.Id == original.Id);
        byte[] initialVersion = original.RowVersion.ToArray();
        original.DisplayName = "Concurrency winner";
        await first.SaveChangesAsync();
        Assert.False(initialVersion.SequenceEqual(original.RowVersion));
        stale.DisplayName = "Stale update";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        original.DedicatedFrontendOrigin = "https://company.certification.example";
        await first.SaveChangesAsync();
        using var collisionScope = provider.CreateScope();
        var collision = (DbContext)collisionScope.ServiceProvider.GetRequiredService(contextType);
        var id = Guid.CreateVersion7();
        collision.Set<Tenant>().Add(new Tenant
        {
            Id = id,
            Identifier = $"collision-{id:N}",
            DisplayName = "Another customer",
            HostName = $"{id:N}.invalid",
            DeploymentStampId = original.DeploymentStampId,
            CreatedBy = "MigrationCertification",
            MaxUsers = 3,
            DedicatedFrontendOrigin = original.DedicatedFrontendOrigin
        });

        var conflict = await Assert.ThrowsAsync<DbUpdateException>(() => collision.SaveChangesAsync());
        Assert.Contains("UX_Tenants_DedicatedFrontendOrigin", conflict.InnerException!.Message, StringComparison.Ordinal);
    }

    private sealed class MigrationTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId
        {
            get; set;
        }
    }
    private sealed class MigrationActorProvider : ICurrentActorProvider
    {
        public string ActorId => "MigrationCertification";
    }

}
