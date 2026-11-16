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

internal sealed class HybridCacheService(HybridCache cache, ILogger<HybridCacheService> logger, TimeProvider timeProvider) : ICacheService
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

        var start = timeProvider.GetTimestamp();

        try
        {
            var options = expiration.HasValue
                ? new HybridCacheEntryOptions
                {
                    Expiration = expiration,
                    LocalCacheExpiration = expiration
                }
                : null;

            // HybridCache may dispatch cancellable factories without their caller's ExecutionContext.
            // Keep the owning request alive until its scoped factory finishes, and flow its identity.
            using var executionContext = ExecutionContext.Capture();
            T result = default!;

            for (var attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    result = await cache.GetOrCreateAsync(
                        key,
                        async _ =>
                        {
                            Task<T>? pending = null;

                            if (executionContext is null)
                            {
                                pending = factory(ct);
                            }
                            else
                            {
                                using var copy = executionContext.CreateCopy();
                                ExecutionContext.Run(copy, _ => pending = factory(ct), null);
                            }
                            return await pending!.ConfigureAwait(false);
                        },
                        options,
                        // Caller cancellation still reaches its factory's I/O. It must not detach
                        // that factory from the lifetime of the request services it owns.
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    ct.ThrowIfCancellationRequested();
                    break;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested && attempt < 2)
                {
                    // A coalesced factory's owner was canceled. Retry in this live caller's scope.
                }
            }

            var elapsedMs = timeProvider.GetElapsedTime(start).TotalMilliseconds;
            activity?.SetTag("cache.latency.ms", elapsedMs);

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
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

        if (expiration.HasValue)
        {
            activity?.SetTag("cache.expiration.seconds", expiration.Value.TotalSeconds);
        }

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
            var flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite;

            if (key.Contains(":version:", StringComparison.Ordinal))
            {
                // Every replica must observe the current mutation epoch from the shared store.
                flags |= HybridCacheEntryFlags.DisableLocalCacheRead;
            }
            return await cache.GetOrCreateAsync<T?>(
                key,
                static _ => ValueTask.FromResult(default(T)),
                new HybridCacheEntryOptions { Flags = flags },
                cancellationToken: ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.AddException(ex);
            ServiceLogDefinitions.LogCacheOperationError(logger, "GET", key, ex);
            throw;
        }
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
    {
        throw new NotSupportedException("Use version tokens instead.");
    }
}
