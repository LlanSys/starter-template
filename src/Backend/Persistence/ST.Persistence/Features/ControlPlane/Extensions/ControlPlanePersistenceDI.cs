using ST.Persistence.Common.Configuration;
using ST.Persistence.Features.Shared.Migrations.Generators;
using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using ST.Persistence.Features.ControlPlane.Auditing.Repositories;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.ControlPlane.Tenants.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;

namespace ST.Persistence.Features.ControlPlane.Extensions;

public static class ControlPlanePersistenceDI
{
    public static IServiceCollection AddControlPlanePersistence(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("ControlPlaneConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ControlPlaneConnection (or DefaultConnection) not found.");

        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        var dbSettings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>() ?? new DatabaseSettings();

        void ConfigureDbContextOptions(DbContextOptionsBuilder options)
        {
            if (dbSettings.Provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString, pgOptions =>
                {
                    pgOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                    pgOptions.CommandTimeout(30);
                    pgOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                    pgOptions.MigrationsHistoryTable("__EFMigrationsHistory_ControlPlane");
                }).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>();
            }
            else
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_ControlPlane");
                }).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
            }

            if (environment?.IsDevelopment() == true || environment?.IsStaging() == true)
            {
                options.EnableSensitiveDataLogging();
            }
        }

        if (dbSettings.Provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ControlPlaneDBContext, ControlPlanePostgreSqlDBContext>(ConfigureDbContextOptions);
        }
        else
        {
            services.AddDbContext<ControlPlaneDBContext, ControlPlaneSqlServerDBContext>(ConfigureDbContextOptions);
        }

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IDeploymentStampRepository, DeploymentStampRepository>();
        services.AddScoped<IImpersonationRecordRepository, ImpersonationRecordRepository>();
        services.AddScoped<IControlPlaneUnitOfWork, ControlPlaneUnitOfWork>();

        return services;
    }
}
