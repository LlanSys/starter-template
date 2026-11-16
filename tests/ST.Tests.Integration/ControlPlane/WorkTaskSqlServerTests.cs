using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

public sealed class WorkTaskSqlServerTests(MsSqlDbFixture fixture) : WorkTaskProviderTests<MsSqlDbFixture>(fixture);
