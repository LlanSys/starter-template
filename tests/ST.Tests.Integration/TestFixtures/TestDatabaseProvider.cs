using Microsoft.EntityFrameworkCore;
// <db:SqlServer>
using Microsoft.Data.SqlClient;
// </db:SqlServer>
// <db:PostgreSql>
using Npgsql;
// </db:PostgreSql>
using ST.Domain.Shared;

namespace ST.Tests.Integration.TestFixtures;

/// <summary>
/// Provider helpers for shared integration tests. Provider branches sit in <c>db:</c> marker
/// blocks so single-provider forks keep only their own branch.
/// </summary>
internal static class TestDatabaseProvider
{
    /// <summary>Configures a builder for the build's primary provider (<see cref="DatabaseProviders.Supported"/>[0]).</summary>
    public static DbContextOptionsBuilder<TContext> UseTestDatabaseProvider<TContext>(
        this DbContextOptionsBuilder<TContext> builder, string connectionString)
        where TContext : DbContext
    {
        var provider = DatabaseProviders.Supported[0];
        // <db:SqlServer>
        if (DatabaseProviders.Is(provider, DatabaseProviders.SqlServer))
        {
            builder.UseSqlServer(connectionString);
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (DatabaseProviders.Is(provider, DatabaseProviders.PostgreSql))
        {
            builder.UseNpgsql(connectionString);
        }
        // </db:PostgreSql>
        return builder;
    }

    /// <summary>Returns the fixture's provider name.</summary>
    public static string ProviderOf(DbFixture fixture)
    {
        // <db:SqlServer>
        if (fixture is MsSqlDbFixture)
        {
            return DatabaseProviders.SqlServer;
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (fixture is PostgreSqlDbFixture)
        {
            return DatabaseProviders.PostgreSql;
        }
        // </db:PostgreSql>
        throw Unsupported(fixture);
    }

    /// <summary>Returns the fixture connection string pointed at a different database name.</summary>
    public static string WithDatabase(DbFixture fixture, string database)
    {
        // <db:SqlServer>
        if (fixture is MsSqlDbFixture)
        {
            return new SqlConnectionStringBuilder(fixture.GetConnectionString()) { InitialCatalog = database }.ConnectionString;
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (fixture is PostgreSqlDbFixture)
        {
            return new NpgsqlConnectionStringBuilder(fixture.GetConnectionString()) { Database = database }.ConnectionString;
        }
        // </db:PostgreSql>
        throw Unsupported(fixture);
    }

    public static NotSupportedException Unsupported(DbFixture fixture) =>
        new($"No database provider branch handles fixture '{fixture.GetType().Name}'.");
}
