using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

public sealed class WorkTaskPostgreSqlTests(PostgreSqlDbFixture fixture) : WorkTaskProviderTests<PostgreSqlDbFixture>(fixture);
