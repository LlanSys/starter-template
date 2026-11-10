using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration;

public sealed class TenantDatabasePostgreSqlReadinessTests(PostgreSqlDbFixture fixture)
    : TenantDatabaseReadinessTests<PostgreSqlDbFixture>(fixture);
