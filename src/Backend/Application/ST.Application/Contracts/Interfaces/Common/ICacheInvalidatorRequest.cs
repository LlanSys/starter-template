using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Contracts.Interfaces.Common;

/// <summary>
/// Marks a MediatR command as one that should invalidate cache entries after it executes.
/// The <see cref="CacheInvalidationBehavior{TRequest,TResponse}"/> pipeline runs AFTER
/// the handler succeeds and performs two types of invalidation:
///
///   1. Direct key deletion   — removes individual entity entries by exact key.
///   2. Version token bump    — increments the version sentinel for a cache group,
///                              instantly orphaning every versioned list/search entry
///                              in that group without iterating or pattern-matching.
///
/// Implement this on write commands: Create*, Update*, Delete*, Approve*, etc.
/// For read queries, implement <see cref="ICachableRequest"/> instead.
/// </summary>
public interface ICacheInvalidatorRequest
{
    /// <summary>
    /// Exact cache keys to delete immediately after the command succeeds.
    /// Use for entity-level entries whose key is known at command time.
    /// Example: CacheKeys.Entity("iam-admin-users", command.UserId)
    /// </summary>
    IReadOnlyList<string> DirectInvalidationKeys => [];

    /// <summary>
    /// Version sentinel keys to bump after the command succeeds.
    /// Each bump orphans ALL versioned list/search entries for the corresponding group and user scope.
    /// Example: CacheKeys.GroupVersion("customers", command.UserId)
    ///          CacheKeys.GroupVersion("customers")        ← global scope
    /// </summary>
    IReadOnlyList<string> GroupVersionKeysToInvalidate => [];
}
