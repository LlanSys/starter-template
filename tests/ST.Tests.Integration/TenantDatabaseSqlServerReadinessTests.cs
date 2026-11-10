using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration;

public sealed class TenantDatabaseSqlServerReadinessTests(MsSqlDbFixture fixture)
    : TenantDatabaseReadinessTests<MsSqlDbFixture>(fixture);
