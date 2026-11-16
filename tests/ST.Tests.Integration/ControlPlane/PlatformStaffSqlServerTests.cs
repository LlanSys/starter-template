using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

public sealed class PlatformStaffSqlServerTests(MsSqlDbFixture fixture) : PlatformStaffMigrationTests<MsSqlDbFixture>(fixture);
