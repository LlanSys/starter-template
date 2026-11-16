using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.ApiTests;

public sealed class SqlServerOrganizationSettingTests(MsSqlDbFixture fixture) : OrganizationSettingProviderTests<MsSqlDbFixture>(fixture);
