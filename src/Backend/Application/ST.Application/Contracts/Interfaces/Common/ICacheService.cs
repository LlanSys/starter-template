using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Contracts.Interfaces.Common;

/// Abstraction over the cache store. Async-first; all operations accept a CancellationToken.
///
/// Implementations:
///   - InMemoryCacheService  (IMemoryCache  — single node, no serialization overhead)
///   - DistributedCacheService (IDistributedCache — Redis, SQL, etc.)
///
/// The interface intentionally has no dependency on any infrastructure type
/// (no MemoryCacheEntryOptions, no IDistributedCache). Infrastructure specifics
/// belong in the implementation, not the contract.
/// </summary>
public interface ICacheService
{
    // ── Write ─────────────────────────────────────────────────────────────────
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    // ── Read ──────────────────────────────────────────────────────────────────
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    // ── Invalidate ────────────────────────────────────────────────────────────

    /// <summary>Removes a single key.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Removes all keys that begin with the given prefix.
    /// Prefer version-token invalidation over this; pattern removal is O(n) and
    /// not supported atomically by all distributed stores.
    /// Only use when you genuinely need to purge a named subset of entries.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>
    /// Gets or creates a cache entry. Uses a factory to fetch data on miss,
    /// protecting against cache stampedes.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}
