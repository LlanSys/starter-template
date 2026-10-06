using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Contracts.Interfaces.Common;

/// <summary>
/// Marks a MediatR query as cache-eligible.
/// The <see cref="CachingBehavior{TRequest,TResponse}"/> pipeline reads these properties
/// to assemble the cache key and decide whether to read/write.
///
/// Key assembly rules (enforced by the behavior — NOT by the request):
///   Non-versioned  → "{group}:entity:{discriminator}"
///   Versioned      → "{group}:list:{userId|global}:{versionToken}:{discriminator}"
///
/// Implement this on read queries (Get*, Search*, List*).
/// For mutations, implement <see cref="ICacheInvalidatorRequest"/> instead.
/// </summary>
public interface ICachableRequest
{
    /// <summary>
    /// Logical group this request belongs to, e.g. "customers", "employees".
    /// Used as the root namespace in every cache key produced for this request,
    /// and as the scope for version token lookups when <see cref="IsVersioned"/> is true.
    /// </summary>
    string CacheGroup { get; }

    /// <summary>
    /// Differentiates entries within the group.
    /// For entity lookups:  use the entity ID string.
    /// For list/search:     use a hash of the filter/pagination parameters.
    ///                      See <see cref="Utilities.CacheKeys.HashFilter"/>.
    /// </summary>
    //string CacheDiscriminator { get; }
    string Discriminator { get; }

    /// <summary>
    /// Scopes the cache entry to a specific user.
    /// Null → the entry is shared across all users (suitable for entity lookups
    /// or data that is not user-filtered at the query level).
    /// </summary>
    string? CacheUserId { get; }

    /// <summary>
    /// True → include a version token in the key.
    /// This enables O(1) bulk invalidation: bumping the version token in
    /// <see cref="CacheInvalidationBehavior{TRequest,TResponse}"/> instantly
    /// orphans every versioned list entry in the group without scanning the cache.
    /// Use true for list/search queries.  Use false for single-entity lookups.
    /// </summary>
    bool IsVersioned { get; }

    /// <summary>
    /// Return true to skip the cache entirely for this invocation.
    /// Useful for administrative/export calls where stale data is unacceptable.
    /// Defaults to false (always cache).
    /// </summary>
    bool BypassCache => false;

    /// <summary>How long the cached value should live. Defaults to 30 minutes.</summary>
    TimeSpan? Expiration => TimeSpan.FromMinutes(30);
}
