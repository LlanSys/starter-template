using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.ControlPlane.Extensions;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.IAM.Extensions;
using ST.Persistence.Features.Shared.DataContext;
using ST.Persistence.Features.Shared.Extensions;
using ST.Persistence.Features.Shared.Migrations.Generators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Microsoft.Extensions.FileProviders;

namespace ST.Tests.Architecture.Database;

[Trait("Category", "Guardrail")]
public class IdempotentMigrationsGuardrailTests
{
    [Theory]
    [InlineData(typeof(SharedSqlServerDBContext), typeof(IdempotentSqlServerMigrationsSqlGenerator))]
    [InlineData(typeof(IamSqlServerDBContext), typeof(IdempotentSqlServerMigrationsSqlGenerator))]
    [InlineData(typeof(ControlPlaneSqlServerDBContext), typeof(IdempotentSqlServerMigrationsSqlGenerator))]
    public void SqlServerContexts_ShouldUseIdempotentMigrationsGenerator(Type contextType, Type expectedGeneratorType)
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        var config = BuildConfiguration("SqlServer");

        var env = new DummyHostEnvironment();

        services.AddSharedPersistence(config, env);
        services.AddIamPersistence(config, env);
        services.AddControlPlanePersistence(config, env);

        services.AddScoped<ICurrentTenantProvider, DummyTenantProvider>();
        services.AddScoped<ICurrentActorProvider, DummyActorProvider>();
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        using var context = (DbContext)provider.GetRequiredService(contextType);

        var generator = context.Database.GetService<IMigrationsSqlGenerator>();
        Assert.IsType(expectedGeneratorType, generator);
    }

    [Theory]
    [InlineData(typeof(SharedPostgreSqlDBContext), typeof(IdempotentNpgsqlMigrationsSqlGenerator))]
    [InlineData(typeof(IamPostgreSqlDBContext), typeof(IdempotentNpgsqlMigrationsSqlGenerator))]
    [InlineData(typeof(ControlPlanePostgreSqlDBContext), typeof(IdempotentNpgsqlMigrationsSqlGenerator))]
    public void PostgreSqlContexts_ShouldUseIdempotentMigrationsGenerator(Type contextType, Type expectedGeneratorType)
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        var config = BuildConfiguration("PostgreSql");

        var env = new DummyHostEnvironment();

        services.AddSharedPersistence(config, env);
        services.AddIamPersistence(config, env);
        services.AddControlPlanePersistence(config, env);

        services.AddScoped<ICurrentTenantProvider, DummyTenantProvider>();
        services.AddScoped<ICurrentActorProvider, DummyActorProvider>();
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        using var context = (DbContext)provider.GetRequiredService(contextType);

        var generator = context.Database.GetService<IMigrationsSqlGenerator>();
        Assert.IsType(expectedGeneratorType, generator);

        var historySql = context.GetService<IHistoryRepository>().GetCreateIfNotExistsScript();
        Assert.Contains("CREATE TABLE IF NOT EXISTS", historySql, StringComparison.Ordinal);
        Assert.DoesNotContain("IF NOT EXISTS IF NOT EXISTS", historySql, StringComparison.Ordinal);
    }

    private static IConfiguration BuildConfiguration(string provider)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=.;Database=dummy;Integrated Security=true" },
                { "DatabaseSettings:Provider", provider }
            })
            .Build();
    }

    private sealed class DummyHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "BT";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class DummyTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId => Guid.Empty;
    }

    private sealed class DummyActorProvider : ICurrentActorProvider
    {
        public string ActorId => "System";
    }
}
