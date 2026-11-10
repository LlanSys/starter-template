using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Npgsql;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.DataContext;
using ST.SharedKernel.Dtos.Common;

namespace ST.Infrastructure.Features.ControlPlane.Provisioning;

internal sealed partial class TenantDatabaseReadiness(
    TimeProvider clock,
    ILogger<TenantDatabaseReadiness> logger) : ITenantDatabaseReadiness
{
    public async Task<AppResponse<bool>> VerifyAsync(
        Guid tenantId,
        string connectionString,
        string provider,
        CancellationToken cancellationToken,
        bool requireIsolated = true)
    {
        try
        {
            await using var iam = CreateIam(connectionString, provider);
            await using var shared = CreateShared(connectionString, provider);
            foreach (var context in new DbContext[] { iam, shared })
            {
                if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
                {
                    return AppResponses.Failure<bool>("Apply all destination workspace migrations before completing this operation.");
                }

                if (requireIsolated && await ContainsForeignWorkspaceAsync(context, tenantId, cancellationToken).ConfigureAwait(false))
                {
                    return AppResponses.Failure<bool>("The destination contains data owned by another workspace.");
                }
            }

            return AppResponses.Success(true);
        }
        catch (Exception exception) when (exception is DbException or ArgumentException or InvalidOperationException)
        {
            LogDestinationUnavailable(logger, exception);
            return AppResponses.Failure<bool>("The destination workspace database could not be verified.");
        }
    }

    private IamDBContext CreateIam(string connection, string provider)
    {
        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var settings = new SqlConnectionStringBuilder(connection);
            ValidateDatabase(settings.InitialCatalog, settings.DataSource);
            return new IamSqlServerDBContext(
                new DbContextOptionsBuilder<IamSqlServerDBContext>()
                    .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_IAM")).Options, clock);
        }

        if (string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            var settings = new NpgsqlConnectionStringBuilder(connection);
            ValidateDatabase(settings.Database, settings.Host);
            return new IamPostgreSqlDBContext(
                new DbContextOptionsBuilder<IamPostgreSqlDBContext>()
                    .UseNpgsql(connection, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory_IAM")).Options, clock);
        }

        throw new ArgumentException("Unsupported destination database provider.");
    }

    private SharedDBContext CreateShared(string connection, string provider)
    {
        return string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase)
            ? new SharedSqlServerDBContext(
                new DbContextOptionsBuilder<SharedSqlServerDBContext>()
                    .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Shared")).Options,
                clock, null!, null!)
            : new SharedPostgreSqlDBContext(
                new DbContextOptionsBuilder<SharedPostgreSqlDBContext>()
                    .UseNpgsql(connection, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory_Shared")).Options,
                clock, null!, null!);
    }

    private static void ValidateDatabase(string? database, string? server)
    {
        string[] reserved = ["master", "model", "msdb", "tempdb", "postgres", "template0", "template1"];
        if (string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(server)
            || reserved.Contains(database, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Choose an explicit workspace database.");
        }
    }

    private static async Task<bool> ContainsForeignWorkspaceAsync(
        DbContext context,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var postgres = context.Database.IsNpgsql();
        string Quote(string identifier) => postgres
            ? "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";

        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandTimeout = 30;
        var tenantParameter = command.CreateParameter();
        tenantParameter.ParameterName = "@tenant";
        tenantParameter.Value = tenantId;
        command.Parameters.Add(tenantParameter);
        var platformParameter = command.CreateParameter();
        platformParameter.ParameterName = "@platform";
        platformParameter.Value = Tenant.PlatformTenantId;
        command.Parameters.Add(platformParameter);

        var queries = new List<string>();
        foreach (var entity in context.Model.GetEntityTypes())
        {
            var property = entity.FindProperty("TenantId");
            var table = entity.GetTableName();
            if (property?.ClrType != typeof(Guid) || table is null || entity.BaseType is not null)
            {
                continue;
            }

            var schema = entity.GetSchema();
            var column = property.GetColumnName(StoreObjectIdentifier.Table(table, schema))!;
            var qualified = schema is null ? Quote(table) : Quote(schema) + "." + Quote(table);
            // These shipped metadata tables contain platform defaults in a freshly migrated database.
            var platformDefaults = table is "MenuItems" or "MenuIcons" or "MenuPlacements" or "MenuRoutes"
                or "Permissions" or "PermissionActions" or "PermissionContexts" or "PermissionResources" or "EmailTemplates";
            queries.Add("SELECT 1 FROM " + qualified + " WHERE " + Quote(column) + " <> @tenant"
                + (platformDefaults ? " AND " + Quote(column) + " <> @platform" : string.Empty));
        }

        if (queries.Count == 0)
        {
            return false;
        }

        command.CommandText = "SELECT CASE WHEN EXISTS (" + string.Join(" UNION ALL ", queries) + ") THEN 1 ELSE 0 END";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Destination workspace database verification failed.")]
    private static partial void LogDestinationUnavailable(ILogger logger, Exception exception);
}
