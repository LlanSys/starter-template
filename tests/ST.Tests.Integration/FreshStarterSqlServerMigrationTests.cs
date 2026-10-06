using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration;

public sealed class FreshStarterSqlServerMigrationTests(MsSqlDbFixture fixture) : FreshStarterMigrationTests<MsSqlDbFixture>(fixture);
