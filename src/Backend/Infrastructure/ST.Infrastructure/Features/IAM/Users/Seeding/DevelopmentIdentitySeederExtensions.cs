using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ST.Infrastructure.Features.IAM.Users.Seeding;

public static class DevelopmentIdentitySeederExtensions
{
    public static async Task SeedDevelopmentIdentityAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var scope = app.Services.CreateAsyncScope();
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentIdentitySeeder>();
            await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }
}
