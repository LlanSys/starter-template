using Microsoft.Extensions.Configuration;

namespace ST.Persistence.Common.DesignTime;

internal static class DesignTimeConfigurationFactory
{
    private const string ApiUserSecretsId = "01a11019-0c5a-7f50-95f0-b8296dc2c445";

    public static IConfigurationRoot Create()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile(@"src\Backend\Api\ST.Api\appsettings.json", optional: true)
            .AddJsonFile(@"src\Backend\Api\ST.Api\appsettings.Development.json", optional: true)
            .AddJsonFile(GetUserSecretsPath(), optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public static string GetConnectionString(IConfiguration configuration, string contextConnectionName, string fallbackConnectionName = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(contextConnectionName)
            ?? configuration.GetConnectionString(fallbackConnectionName)
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.StartsWith("SET_VIA_", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("REPLACE_VIA_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"A valid connection string was not found for '{contextConnectionName}' or 'DefaultConnection'. " +
                "Set it via user-secrets or environment variables before running EF Core tooling.");
        }

        return connectionString;
    }

    public static void ConfigureSqlServer(
        Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder sqlOptions,
        string migrationsHistoryTable)
    {
        ArgumentNullException.ThrowIfNull(sqlOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationsHistoryTable);

        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        sqlOptions.MigrationsHistoryTable(migrationsHistoryTable);
    }

    public static void ConfigurePostgreSql(
        Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder pgOptions,
        string migrationsHistoryTable)
    {
        ArgumentNullException.ThrowIfNull(pgOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationsHistoryTable);

        pgOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        pgOptions.MigrationsHistoryTable(migrationsHistoryTable);
    }

    private static string GetUserSecretsPath()
    {
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return Path.Combine(applicationData, "Microsoft", "UserSecrets", ApiUserSecretsId, "secrets.json");
    }
}
