using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace ST.Tests.Integration.TestFixtures;

public class PostgreSqlDbFixture : DbFixture
{
    private readonly PostgreSqlContainer _dbContainer;

    public PostgreSqlDbFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public override string GetConnectionString()
    {
        return _dbContainer.GetConnectionString();
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
