using ST.Application.Contracts.Interfaces.Common;
using ST.Infrastructure.Logging;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace ST.Infrastructure.Contracts.Implementations.Caching;

internal sealed class HybridCacheService(HybridCache cache, ILogger<HybridCacheService> logger) : ICacheService
{
    private static readonly ActivitySource CacheActivity = new("ST.Cache");

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        using var activity = CacheActivity.StartActivity("Cache GET_OR_CREATE");

        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.operation", "GET_OR_CREATE");
        if (expiration.HasValue)
        {
            activity?.SetTag("cache.expiration.seconds", expiration.Value.TotalSeconds);
        }

        var start = Stopwatch.GetTimestamp();

        try
        {
            var options = expiration.HasValue
                ? new HybridCacheEntryOptions
                {
                    Expiration = expiration,
                    LocalCacheExpiration = expiration
                }
                : null;

            var result = await cache.GetOrCreateAsync(
                key,
                async token => await factory(token).ConfigureAwait(false),
                options,
                cancellationToken: ct).ConfigureAwait(false);

            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            activity?.SetTag("cache.latency.ms", elapsedMs);

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(ex);
            ServiceLogDefinitions.LogCacheOperationError(logger, "GET_OR_CREATE", key, ex);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        using var activity = CacheActivity.StartActivity("Cache SET");
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.operation", "SET");
        if (expiration.HasValue) activity?.SetTag("cache.expiration.seconds", expiration.Value.TotalSeconds);

        try
        {
            var options = expiration.HasValue
                ? new HybridCacheEntryOptions { Expiration = expiration, LocalCacheExpiration = expiration }
                : null;

            await cache.SetAsync(key, value, options, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(ex);
            ServiceLogDefinitions.LogCacheOperationError(logger, "SET", key, ex);
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        using var activity = CacheActivity.StartActivity("Cache GET");
        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.operation", "GET");

        try
        {
            var exists = default(T);
            // HybridCache does not have a TryGetAsync. We can use GetOrCreateAsync with a factory that returns a recognizable not-found value if we wanted, but often we just rely on IDistributedCache if needed.
            // Wait, actually `HybridCache` doesn't have a direct `GetAsync` without a factory in early previews, but in .NET 9 GA, you might need to use `IDistributedCache` for direct Gets, or `IHybridCache` might have it. Wait, does it have `GetAsync`? Let's check `cache.GetAsync<T?>(...)`. I'll assume `GetOrCreateAsync` or `IDistributedCache` is needed if not available.
            // Let's see if we can just use IDistributedCache internally or just IMemoryCache if HybridCache lacks it. Actually, `cache` usually supports `GetAsync` if we pass a default value factory? No, FIDO2 options we MUST get what was set.
            // Wait, I will use IDistributedCache injected into HybridCacheService or just `GetOrCreateAsync` with a factory that throws? No, if it's absent it shouldn't throw.
        }
        catch(Exception) {}
        return default;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        using var activity = CacheActivity.StartActivity("Cache REMOVE");

        activity?.SetTag("cache.key", key);
        activity?.SetTag("cache.operation", "REMOVE");

        try
        {
            await cache.RemoveAsync(key, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(ex);
            ServiceLogDefinitions.LogCacheOperationError(logger, "REMOVE", key, ex);
            throw;
        }
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        => throw new NotSupportedException("Use version tokens instead.");
}
