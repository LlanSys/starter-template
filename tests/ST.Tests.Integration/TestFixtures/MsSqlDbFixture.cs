using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace ST.Tests.Integration.TestFixtures;

public class MsSqlDbFixture : DbFixture
{
    private readonly MsSqlContainer _dbContainer;

    public MsSqlDbFixture()
    {
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Password123!")
            .Build();
    }

    public override string GetConnectionString()
    {
        var cs = _dbContainer.GetConnectionString();
        return cs + (cs.EndsWith(";") ? "" : ";") + "TrustServerCertificate=True;Encrypt=False;";
    }

    public override async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public override async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
