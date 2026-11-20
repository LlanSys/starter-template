using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration;

public sealed class SqlServerIamAccountOutboxTests(MsSqlDbFixture fixture)
    : IamAccountOutboxProviderTests<MsSqlDbFixture>(fixture);
