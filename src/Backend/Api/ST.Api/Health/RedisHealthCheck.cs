using ST.Infrastructure.Configuration;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

namespace ST.Api.Health;

internal sealed class RedisHealthCheck(
    IOptions<CacheSettings> options,
    IServiceProvider serviceProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (IsMemoryCache(settings))
        {
            return HealthCheckResult.Healthy("Redis is not selected for the current cache provider.");
        }

        var multiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
        if (multiplexer is null)
        {
            return HealthCheckResult.Unhealthy("Redis cache provider is selected but IConnectionMultiplexer is not registered.");
        }

        try
        {
            var database = multiplexer.GetDatabase();
            var latency = await database.PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy($"Redis is reachable. Ping={latency.TotalMilliseconds:0.##}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis probe failed.", ex);
        }
    }

    private static bool IsMemoryCache(CacheSettings settings)
    {
        return string.Equals(settings.Provider, "Memory", StringComparison.OrdinalIgnoreCase) || (string.Equals(settings.Provider, "Auto", StringComparison.OrdinalIgnoreCase) &&
               string.IsNullOrWhiteSpace(settings.Redis.ConnectionString) &&
               string.IsNullOrWhiteSpace(settings.Azure?.ConnectionString));
    }
}
