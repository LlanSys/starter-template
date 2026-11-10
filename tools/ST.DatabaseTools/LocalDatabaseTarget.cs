using System.Net;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.Migrations.Generators;

namespace ST.DatabaseTools;

internal sealed class LocalDatabaseTarget
{
    private readonly string _connection;

    public LocalDatabaseTarget(bool sqlServer, string connection)
    {
        SqlServer = sqlServer;
        string? host;
        string? database;

        if (sqlServer)
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
        }
        else
        {
            var settings = new NpgsqlConnectionStringBuilder(connection);
            host = settings.Host;
            database = settings.Database;
        }

        var localSqlAlias = sqlServer && (host == "." || string.Equals(host, "(local)", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "(localdb)", StringComparison.OrdinalIgnoreCase));

        if (!IsLoopback(host) && !localSqlAlias)
        {
            throw new LocalDatabaseOperationException("Choose a single local database server. Remote database targets are refused.");
        }

        string[] reserved = ["master", "model", "msdb", "tempdb", "postgres", "template0", "template1"];
        if (string.IsNullOrWhiteSpace(database) || reserved.Contains(database, StringComparer.OrdinalIgnoreCase))
        {
            throw new LocalDatabaseOperationException("Choose an explicit local application database.");
        }

        _connection = connection;
    }

    public bool SqlServer { get; }

    public static LocalDatabaseTarget ResolveForMigrations(bool sqlServer)
    {
        var explicitConnection = Environment.GetEnvironmentVariable("ST_DATABASE_CONNECTION");
        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            return new LocalDatabaseTarget(sqlServer, explicitConnection);
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

        var provider = sqlServer ? "SqlServer" : "PostgreSql";
        if (!string.Equals(configuration["DatabaseSettings:Provider"], provider, StringComparison.OrdinalIgnoreCase))
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

        return new LocalDatabaseTarget(sqlServer, connection);
    }

    public IamDBContext CreateIamContext(TimeProvider clock)
    {
        return SqlServer
            ? new IamSqlServerDBContext(CreateOptions<IamSqlServerDBContext>("IAM"), clock)
            : new IamPostgreSqlDBContext(CreateOptions<IamPostgreSqlDBContext>("IAM"), clock);
    }

    public DbContextOptions<TContext> CreateOptions<TContext>(string contextName) where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>();
        var historyTable = "__EFMigrationsHistory_" + contextName;
        if (SqlServer)
        {
            options.UseSqlServer(_connection, sql => sql.MigrationsHistoryTable(historyTable))
                .ReplaceService<IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
        }
        else
        {
            options.UseNpgsql(_connection, postgres => postgres.MigrationsHistoryTable(historyTable))
                .ReplaceService<IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>();
        }

        return options.Options;
    }

    internal static bool IsLoopback(string? host)
    {
        host = host?.Trim('[', ']');
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
    }
}
