using System.Net;
using System.Xml.Linq;
// <db:SqlServer>
using Microsoft.Data.SqlClient;
// </db:SqlServer>
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
// <db:PostgreSql>
using Npgsql;
// </db:PostgreSql>
using ST.Domain.Shared;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.DataContext;
using ST.Persistence.Features.Shared.Migrations.Generators;

namespace ST.DatabaseTools;

internal sealed class LocalDatabaseTarget
{
    private readonly string _connection;

    public LocalDatabaseTarget(string provider, string connection)
    {
        Provider = DatabaseProviders.Normalize(provider);
        string? host = null;
        string? database = null;
        var localAlias = false;
        List<string> reserved = [];

        // <db:SqlServer>
        if (Is(DatabaseProviders.SqlServer))
        {
            var settings = new SqlConnectionStringBuilder(connection);
            var server = settings.DataSource;
            server = server.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase) ? server[4..] : server;
            host = server.Split([',', '\\'], 2)[0].Trim();
            database = settings.InitialCatalog;

            if (!string.IsNullOrEmpty(settings.FailoverPartner) || !string.IsNullOrEmpty(settings.AttachDBFilename)
                || settings.UserInstance)
            {
                throw new LocalDatabaseOperationException("Choose a directly addressed local SQL Server database.");
            }

            localAlias = host == "." || string.Equals(host, "(local)", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "(localdb)", StringComparison.OrdinalIgnoreCase);
            reserved.AddRange(["master", "model", "msdb", "tempdb"]);
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (Is(DatabaseProviders.PostgreSql))
        {
            var settings = new NpgsqlConnectionStringBuilder(connection);
            host = settings.Host;
            database = settings.Database;
            reserved.AddRange(["postgres", "template0", "template1"]);
        }
        // </db:PostgreSql>

        if (!IsLoopback(host) && !localAlias)
        {
            throw new LocalDatabaseOperationException("Choose a single local database server. Remote database targets are refused.");
        }

        if (string.IsNullOrWhiteSpace(database) || reserved.Contains(database, StringComparer.OrdinalIgnoreCase))
        {
            throw new LocalDatabaseOperationException("Choose an explicit local application database.");
        }

        _connection = connection;
    }

    public string Provider { get; }

    private bool Is(string provider) => DatabaseProviders.Is(Provider, provider);

    public static LocalDatabaseTarget ResolveForMigrations(string provider)
    {
        var explicitConnection = Environment.GetEnvironmentVariable("ST_DATABASE_CONNECTION");
        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            return new LocalDatabaseTarget(provider, explicitConnection);
        }

        var apiDirectory = Path.GetFullPath("src/Backend/Api/ST.Api");
        var project = Path.Combine(apiDirectory, "ST.Api.csproj");
        if (!File.Exists(project))
        {
            throw new LocalDatabaseOperationException("Run the local database command from the StarterTemplate repository root.");
        }

        var secretsId = XDocument.Load(project).Descendants("UserSecretsId").FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(secretsId))
        {
            throw new LocalDatabaseOperationException("The API project must declare a UserSecretsId for local database configuration.");
        }

        using var configuration = (ConfigurationRoot)new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(secretsId)
            .AddEnvironmentVariables()
            .Build();

        if (!DatabaseProviders.Is(configuration["DatabaseSettings:Provider"], provider))
        {
            throw new LocalDatabaseOperationException("Match -Provider to the API's Development DatabaseSettings:Provider, or select an explicit local database.");
        }

        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection) || connection.StartsWith("SET_VIA_", StringComparison.OrdinalIgnoreCase)
            || connection.StartsWith("REPLACE_VIA_", StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalDatabaseOperationException("Set the API's ConnectionStrings:DefaultConnection in user secrets, or set ST_DATABASE_CONNECTION for this process.");
        }

        string[] contextConnections = ["ControlPlaneConnection", "IamConnection", "SharedConnection"];
        foreach (var name in contextConnections)
        {
            var scopedConnection = configuration.GetConnectionString(name);
            if (scopedConnection is not null && !string.Equals(scopedConnection, connection, StringComparison.Ordinal))
            {
                throw new LocalDatabaseOperationException($"The API overrides {name}. This local workflow requires matching context connections.");
            }
        }

        return new LocalDatabaseTarget(provider, connection);
    }

    public ControlPlaneDBContext CreateControlPlaneContext(TimeProvider clock)
    {
        // <db:SqlServer>
        if (Is(DatabaseProviders.SqlServer))
        {
            return new ControlPlaneSqlServerDBContext(CreateOptions<ControlPlaneSqlServerDBContext>("ControlPlane"), clock);
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (Is(DatabaseProviders.PostgreSql))
        {
            return new ControlPlanePostgreSqlDBContext(CreateOptions<ControlPlanePostgreSqlDBContext>("ControlPlane"), clock);
        }
        // </db:PostgreSql>
        throw new LocalDatabaseOperationException($"Unsupported database provider '{Provider}'.");
    }

    public IamDBContext CreateIamContext(TimeProvider clock)
    {
        // <db:SqlServer>
        if (Is(DatabaseProviders.SqlServer))
        {
            return new IamSqlServerDBContext(CreateOptions<IamSqlServerDBContext>("IAM"), clock);
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (Is(DatabaseProviders.PostgreSql))
        {
            return new IamPostgreSqlDBContext(CreateOptions<IamPostgreSqlDBContext>("IAM"), clock);
        }
        // </db:PostgreSql>
        throw new LocalDatabaseOperationException($"Unsupported database provider '{Provider}'.");
    }

    public SharedDBContext CreateSharedContext(TimeProvider clock, ICurrentTenantProvider tenant, ICurrentActorProvider actor)
    {
        // <db:SqlServer>
        if (Is(DatabaseProviders.SqlServer))
        {
            return new SharedSqlServerDBContext(CreateOptions<SharedSqlServerDBContext>("Shared"), clock, tenant, actor);
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (Is(DatabaseProviders.PostgreSql))
        {
            return new SharedPostgreSqlDBContext(CreateOptions<SharedPostgreSqlDBContext>("Shared"), clock, tenant, actor);
        }
        // </db:PostgreSql>
        throw new LocalDatabaseOperationException($"Unsupported database provider '{Provider}'.");
    }

    public DbContextOptions<TContext> CreateOptions<TContext>(string contextName) where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>();
        var historyTable = "__EFMigrationsHistory_" + contextName;
        // <db:SqlServer>
        if (Is(DatabaseProviders.SqlServer))
        {
            options.UseSqlServer(_connection, sql => sql.MigrationsHistoryTable(historyTable))
                .ReplaceService<IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (Is(DatabaseProviders.PostgreSql))
        {
            options.UseNpgsql(_connection, npgsql => npgsql.MigrationsHistoryTable(historyTable))
                .ReplaceService<IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>();
        }
        // </db:PostgreSql>

        return options.Options;
    }

    internal static bool IsLoopback(string? host)
    {
        host = host?.Trim('[', ']');
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
    }
}
