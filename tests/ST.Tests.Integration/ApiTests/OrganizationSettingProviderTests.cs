using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using ST.Application.Features.Shared.OrgSettings.CommandHandlers;
using ST.Application.Features.Shared.OrgSettings.QueryHandlers;
using ST.Domain.Features.Shared.Contracts;
using ST.Domain.Features.Shared.EmailTemplates.Contracts.Repositories;
using ST.Domain.Features.Shared.FailedMessages.Contracts.Repositories;
using ST.Domain.Features.Shared.Payments.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.Shared;
using ST.Persistence.Features.Shared.DataContext;
using ST.Persistence.Features.Shared.OrgSettings.Repositories;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Common.Enums;
using ST.SharedKernel.Features.Shared.OrgSettings;
using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.ApiTests;

public abstract class OrganizationSettingProviderTests<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : DbFixture
{
    [Fact]
    public async Task ProviderSerializesWritesAndAtomicallyRecordsResetWithCapturedUtcTime()
    {
        var postgres = fixture is PostgreSqlDbFixture;
        var database = "OrgSettings_" + Guid.CreateVersion7().ToString("N");
        var connection = postgres
            ? new NpgsqlConnectionStringBuilder(fixture.GetConnectionString()) { Database = database }.ConnectionString
            : new SqlConnectionStringBuilder(fixture.GetConnectionString()) { InitialCatalog = database }.ConnectionString;

        var tenant = Guid.CreateVersion7();
        TimeProvider clock = new FixedClock();
        SharedDBContext Context(Guid owner)
        {
            return postgres
                ? new SharedPostgreSqlDBContext(new DbContextOptionsBuilder<SharedPostgreSqlDBContext>().UseNpgsql(connection).Options, clock, new TenantScope(owner), new Actor())
                : new SharedSqlServerDBContext(new DbContextOptionsBuilder<SharedSqlServerDBContext>().UseSqlServer(connection).Options, clock, new TenantScope(owner), new Actor());
        }

        await using (var schema = Context(tenant))
        {
            await schema.Database.MigrateAsync();
        }
        async Task<AppResponse<bool>> SendAsync(SaveCatalogOrgSettingCommand command)
        {
            await using var db = Context(tenant);
            var unit = new SharedUnitOfWork(db, Substitute.For<IEmailTemplateRepository>(), Substitute.For<IFailedMessageRepository>(),
                Substitute.For<IPaymentRecordRepository>(), new OrgSettingRepository(db), Substitute.For<IPublisher>(), NullLogger<SharedUnitOfWork>.Instance);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(clock);
            services.AddSingleton<ISharedUnitOfWork>(unit);
            services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(SaveCatalogOrgSettingCommand).Assembly));
            await using var provider = services.BuildServiceProvider();
            return await provider.GetRequiredService<ISender>().Send(command);
        }
        var commands = new[] { "50", "100" }.Select(value => SendAsync(new SaveCatalogOrgSettingCommand(
            OrganizationSettingCatalog.PageSize, value, null, false, "provider-settings-actor")));

        var results = await Task.WhenAll(commands);
        Assert.Single(results.Where(result => result.IsSuccess));
        Assert.Single(results.Where(result => result.Error?.Type == ErrorType.Conflict));
        await using var verify = Context(tenant);
        var setting = await verify.OrgSettings.AsNoTracking().SingleAsync();
        var revision = Convert.ToBase64String(setting.RowVersion);
        var reset = await SendAsync(new SaveCatalogOrgSettingCommand(setting.Key, null, revision, true, "provider-settings-actor"));
        Assert.True(reset.IsSuccess, reset.Message);
        Assert.False(await verify.OrgSettings.AnyAsync());
        var history = await verify.OrgSettingChanges.OrderBy(change => change.Id).ToArrayAsync();
        Assert.Equal(2, history.Length);
        Assert.False(history[0].Reset);
        Assert.True(history[1].Reset);
        Assert.Equal(setting.Value, history[1].PreviousValue);
        Assert.All(history, change => Assert.Equal(clock.GetUtcNow(), change.CreatedAt));
        Assert.All(history, change => Assert.Equal("provider-settings-actor", change.CreatedBy));
        await using var foreign = Context(Guid.CreateVersion7());
        Assert.False(await foreign.OrgSettingChanges.AnyAsync());
    }

    private sealed record TenantScope(Guid TenantId) : ICurrentTenantProvider;
    private sealed class Actor : ICurrentActorProvider
    {
        public string ActorId => "provider-settings-actor";
    }
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
        }
    }
}
