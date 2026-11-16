using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.Features.IAM;
public sealed class TenantAccountAdmissionSqlServerTests(MsSqlDbFixture fixture) : TenantAccountAdmissionProviderTests<MsSqlDbFixture>(fixture);
