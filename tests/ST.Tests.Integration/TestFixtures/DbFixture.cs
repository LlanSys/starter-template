using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration.TestFixtures;

public abstract class DbFixture : IAsyncLifetime
{
    public abstract string GetConnectionString();

    public abstract Task InitializeAsync();

    public abstract Task DisposeAsync();
}
