using System.Data.Common;
using System.Threading.Channels;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ST.Domain.Shared;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.EventHandlers;
using ST.Application.Features.IAM.Users.IntegrationEvents;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;
using ST.Infrastructure.Features.IAM.Authorization;
using ST.Persistence.Features.IAM.Extensions;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.IAM.Users.Stores;
using ST.Persistence.Features.Shared.DataContext;
using ST.Persistence.Features.Shared.Extensions;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.Tests.Integration.TestFixtures;
using ApiRegistration = ST.Api.Extensions.DependencyInjection;

namespace ST.Tests.Integration;

public abstract class IamAccountOutboxProviderTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture : DbFixture
{
    [Fact]
    public async Task AccountAndNoticeRollbackTogetherAndWorkerDrainsBothOutboxesAfterRestart()
    {
        var provider = TestDatabaseProvider.ProviderOf(fixture);
        var database = $"IamOutbox_{Guid.CreateVersion7():N}";
        var connection = TestDatabaseProvider.WithDatabase(fixture, database);

        var tenant = Guid.CreateVersion7();
        var otherTenant = Guid.CreateVersion7();
        var actor = Guid.CreateVersion7().ToString();
        var now = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);
        var legacyUserId = Guid.CreateVersion7().ToString();
        var command = new CreateAppUserCommand(
            new CreateAppUserRequest("qa-account", "qa@example.test", "TestPassword42!", "QA", "Account", null, []), actor);

        var commitFault = new AccountCommitFault();
        Guid iamMessageId;
        Guid sharedMessageId;
        string userId;

        using (var firstProcess = BuildHost(provider, connection, tenant, actor, now, commitFault))
        {
            await using var scope = firstProcess.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var iam = services.GetRequiredService<IamDBContext>();
            var shared = services.GetRequiredService<SharedDBContext>();
            var sender = services.GetRequiredService<ISender>();

            await iam.Database.MigrateAsync();
            await shared.Database.MigrateAsync();

            commitFault.FailNextCommit = true;
            await Assert.ThrowsAsync<CommitInterruptedException>(() => sender.Send(command));

            await using (var observer = firstProcess.Services.CreateAsyncScope())
            {
                var persisted = observer.ServiceProvider.GetRequiredService<IamDBContext>();
                Assert.Empty(await persisted.Users.ToListAsync());
                Assert.Empty(await persisted.AppUserProfiles.ToListAsync());
                Assert.Empty(await persisted.Set<OutboxState>().ToListAsync());
                Assert.Empty(await persisted.Set<OutboxMessage>().ToListAsync());
            }

            var created = await sender.Send(command);
            Assert.True(created.IsSuccess, created.Message);
            userId = Assert.IsType<AppUserResponse>(created.Data).Id;
            var account = await iam.Users.AsNoTracking().SingleAsync();
            Assert.Equal(userId, account.Id);
            Assert.Equal(tenant, account.TenantId);
            Assert.Equal(actor, account.CreatedBy);
            var profile = await iam.AppUserProfiles.AsNoTracking().SingleAsync();
            Assert.Equal(userId, profile.AppUserId);
            Assert.Equal(actor, profile.CreatedBy);
            Assert.False((await sender.Send(command)).IsSuccess);
            iamMessageId = (await iam.Set<OutboxMessage>().AsNoTracking().SingleAsync()).MessageId;
            Assert.Empty(await shared.Set<OutboxMessage>().ToListAsync());

            var legacyNotice = new AppUserCreatedIntegrationEvent(
                legacyUserId, otherTenant, "legacy", "Legacy Account", "legacy@example.test", now);

            await services.GetRequiredService<IPublishEndpoint>().Publish(legacyNotice);
            await shared.SaveChangesAsync();
            sharedMessageId = (await shared.Set<OutboxMessage>().AsNoTracking().SingleAsync()).MessageId;
        }

        using var restarted = BuildHost(provider, connection, otherTenant, actor, now, new AccountCommitFault());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(75));
        var received = restarted.Services.GetRequiredService<Channel<ReceivedNotice>>();
        await restarted.StartAsync(timeout.Token);

        try
        {
            var first = await received.Reader.ReadAsync(timeout.Token);
            var second = await received.Reader.ReadAsync(timeout.Token);
            var notices = new[] { first, second };
            var iamNotice = Assert.Single(notices, delivery => delivery.Id == iamMessageId).Event;
            Assert.Equal(userId, iamNotice.UserId);
            Assert.Equal(tenant, iamNotice.TenantId);
            Assert.Equal(now, iamNotice.OccurredAt);
            var sharedNotice = Assert.Single(notices, delivery => delivery.Id == sharedMessageId).Event;
            Assert.Equal(legacyUserId, sharedNotice.UserId);
            Assert.Equal(otherTenant, sharedNotice.TenantId);

            await using var scope = restarted.Services.CreateAsyncScope();
            var iam = scope.ServiceProvider.GetRequiredService<IamDBContext>();
            var shared = scope.ServiceProvider.GetRequiredService<SharedDBContext>();
            Assert.Empty(await iam.Users.AsNoTracking().ToListAsync(timeout.Token));
            Assert.Empty(await iam.AppUserProfiles.AsNoTracking().ToListAsync(timeout.Token));

            while (await iam.Set<OutboxState>().AnyAsync(timeout.Token) ||
                   await shared.Set<OutboxState>().AnyAsync(timeout.Token))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
            }

            Assert.Empty(await iam.Set<OutboxMessage>().ToListAsync(timeout.Token));
            Assert.Empty(await shared.Set<OutboxMessage>().ToListAsync(timeout.Token));
        }
        finally
        {
            await restarted.StopAsync();
        }
    }

    private static IHost BuildHost(
        string provider, string connection, Guid tenant, string actor, DateTimeOffset now, AccountCommitFault fault)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        }));

        var services = builder.Services;
        services.AddSingleton<TimeProvider>(new TestClock(now));
        services.AddSingleton<ICurrentTenantProvider>(new TenantScope(tenant));
        services.AddSingleton<ICurrentActorProvider>(new ActorScope(actor));
        services.AddSingleton(Substitute.For<IEffectiveModulePolicy>());
        var limits = Substitute.For<ITenantAccountLimitProvider>();
        limits.GetLimitAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<int?>(100));
        services.AddSingleton(limits);
        services.AddSingleton(Channel.CreateUnbounded<ReceivedNotice>());
        services.AddHttpContextAccessor();

        builder.Configuration["DatabaseSettings:Provider"] = provider;
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connection;
        services.AddSingleton(Substitute.For<ITenantConnectionResolver>());
        services.AddIamPersistence(builder.Configuration, builder.Environment);
        services.AddSharedPersistence(builder.Configuration, builder.Environment);

        // <db:PostgreSql>
        if (DatabaseProviders.Is(provider, DatabaseProviders.PostgreSql))
        {
            services.ConfigureDbContext<IamPostgreSqlDBContext>(options => options.AddInterceptors(fault));
        }
        // </db:PostgreSql>
        // <db:SqlServer>
        if (DatabaseProviders.Is(provider, DatabaseProviders.SqlServer))
        {
            services.ConfigureDbContext<IamSqlServerDBContext>(options => options.AddInterceptors(fault));
        }
        // </db:SqlServer>

        services.AddIdentityCore<AppUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<IamDBContext>()
            .AddUserStore<TenantUserStore>();

        services.AddScoped<IamDelegation>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddMediatR(options =>
        {
            options.TypeEvaluator = type => type == typeof(CreateAppUser) || type == typeof(AppUserCreatedEventHandler);
            options.RegisterServicesFromAssemblies(typeof(CreateAppUser).Assembly, typeof(CreateAppUserCommand).Assembly);
        });

        services.AddMassTransit(registration =>
        {
            ApiRegistration.AddDatabaseOutboxes(registration, provider);
            registration.AddConsumer<NoticeConsumer>();
            registration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
        });

        return builder.Build();
    }

    private sealed record TenantScope(Guid TenantId) : ICurrentTenantProvider;

    private sealed record ActorScope(string ActorId) : ICurrentActorProvider;

    private sealed class TestClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    private sealed record ReceivedNotice(Guid? Id, AppUserCreatedIntegrationEvent Event);

    private sealed class NoticeConsumer(Channel<ReceivedNotice> received) : IConsumer<AppUserCreatedIntegrationEvent>
    {
        public async Task Consume(ConsumeContext<AppUserCreatedIntegrationEvent> context)
        {
            await received.Writer.WriteAsync(new ReceivedNotice(context.MessageId, context.Message), context.CancellationToken);
        }
    }

    private sealed class AccountCommitFault : DbTransactionInterceptor
    {
        internal bool FailNextCommit
        {
            get; set;
        }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (FailNextCommit)
            {
                FailNextCommit = false;
                var iam = Assert.IsAssignableFrom<IamDBContext>(eventData.Context);
                var message = Assert.Single(iam.ChangeTracker.Entries<OutboxMessage>());
                Assert.Equal(EntityState.Unchanged, message.State);
                throw new CommitInterruptedException();
            }

            return base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
        }
    }

    private sealed class CommitInterruptedException : InvalidOperationException;
}
