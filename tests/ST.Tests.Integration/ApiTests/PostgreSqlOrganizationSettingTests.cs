using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.ApiTests;

public sealed class PostgreSqlOrganizationSettingTests(PostgreSqlDbFixture fixture) : OrganizationSettingProviderTests<PostgreSqlDbFixture>(fixture);
