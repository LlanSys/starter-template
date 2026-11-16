using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.Features.IAM;
public sealed class TenantAccountAdmissionPostgreSqlTests(PostgreSqlDbFixture fixture) : TenantAccountAdmissionProviderTests<PostgreSqlDbFixture>(fixture);
