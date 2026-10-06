using ST.Persistence.Common.Configuration;
using ST.Persistence.Features.Shared.Migrations.Generators;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Persistence.Features.Banking.DataContext;
using ST.Persistence.Features.Banking.Customers.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using ST.Persistence.Common.Interceptors;

namespace ST.Persistence.Features.Banking.Extensions;

public static class BankingPersistenceDI
{
    public static IServiceCollection AddBankingPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BankingConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("BankingConnection (or DefaultConnection) not found.");

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
                    pgOptions.MigrationsHistoryTable("__EFMigrationsHistory_Banking");
                }).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>();
            }
            else
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Banking");
                }).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
            }

            options.AddInterceptors(provider.GetRequiredService<TenantConnectionInterceptor>());
        }

        if (dbSettings.Provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<BankingDBContext, BankingPostgreSqlDBContext>(ConfigureDbContextOptions);
        }
        else
        {
            services.AddDbContext<BankingDBContext, BankingSqlServerDBContext>(ConfigureDbContextOptions);
        }

        services.AddScoped<ICustomerRepository, BankingCustomerRepository>();
        services.AddScoped<IBankingUnitOfWork, BankingUnitOfWork>();

        return services;
    }
}
