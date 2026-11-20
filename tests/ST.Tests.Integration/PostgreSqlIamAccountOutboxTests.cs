using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration;

public sealed class PostgreSqlIamAccountOutboxTests(PostgreSqlDbFixture fixture)
    : IamAccountOutboxProviderTests<PostgreSqlDbFixture>(fixture);
