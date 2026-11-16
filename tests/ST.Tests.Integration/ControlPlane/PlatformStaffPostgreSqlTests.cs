using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

public sealed class PlatformStaffPostgreSqlTests(PostgreSqlDbFixture fixture) : PlatformStaffMigrationTests<PostgreSqlDbFixture>(fixture);
