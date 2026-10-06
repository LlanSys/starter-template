using ST.Persistence.Common.Configuration;
using ST.Persistence.Features.Shared.Migrations.Generators;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Features.Shared.Payments.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Persistence.Features.Shared;
using ST.Persistence.Features.Shared.DataContext;
using ST.Persistence.Features.Shared.EmailTemplates.Repositories;
using ST.Persistence.Features.Shared.FailedMessages.Repositories;

using ST.Persistence.Features.Shared.Payments.Repositories;
using ST.Domain.Features.Shared.OrgSettings.Contracts.Repositories;
using ST.Persistence.Features.Shared.OrgSettings.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using ST.Persistence.Common.Interceptors;

namespace ST.Persistence.Features.Shared.Extensions;

public static class SharedPersistenceDI
{
    public static IServiceCollection AddSharedPersistence(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("SharedConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("SharedConnection (or DefaultConnection) not found.");

        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        var dbSettings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>() ?? new DatabaseSettings();

        services.TryAddSingleton<TenantConnectionInterceptor>();

        void ConfigureDbContextOptions(IServiceProvider provider, DbContextOptionsBuilder options)
        {
            if (dbSettings.Provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString, pgOptions =>
                {
                    pgOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                    pgOptions.CommandTimeout(30);
                    pgOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                    pgOptions.MigrationsHistoryTable("__EFMigrationsHistory_Shared");
                }).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>();
            }
            else
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Shared");
                }).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
            }

            // Enable sensitive data logging only in non-production environments for diagnostics
            if (environment?.IsDevelopment() == true || environment?.IsStaging() == true)
            {
                options.EnableSensitiveDataLogging();
            }

            options.AddInterceptors(provider.GetRequiredService<TenantConnectionInterceptor>());
        }

        if (dbSettings.Provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<SharedDBContext, SharedPostgreSqlDBContext>(ConfigureDbContextOptions);
        }
        else
        {
            services.AddDbContext<SharedDBContext, SharedSqlServerDBContext>(ConfigureDbContextOptions);
        }


        services.AddScoped<IEmailTemplateRepository, SharedEmailTemplateRepository>();
        services.AddScoped<IFailedMessageRepository, SharedFailedMessageRepository>();
        services.AddScoped<IPaymentRecordRepository, SharedPaymentRecordRepository>();
        services.AddScoped<IOrgSettingRepository, OrgSettingRepository>();
        services.AddScoped<ISharedUnitOfWork, SharedUnitOfWork>();

        return services;
    }
}
