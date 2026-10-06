using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration;

public sealed class FreshStarterPostgreSqlMigrationTests(PostgreSqlDbFixture fixture) : FreshStarterMigrationTests<PostgreSqlDbFixture>(fixture);
