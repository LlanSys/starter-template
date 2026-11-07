using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Shared.Contracts.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ST.Application.Behaviours;

/// <summary>
/// MediatR pipeline behavior that invalidates cache entries after a mutation command succeeds.
/// Runs for any command implementing <see cref="ICacheInvalidatorRequest"/>.
///
/// Invalidation runs AFTER the handler — only on success. If the handler throws,
/// the cache is left untouched and the exception propagates normally.
///
/// Two invalidation modes:
///
///   1. Direct key deletion
///      Removes entity-level entries whose exact key is known at command time.
///      Example: the entity the command just updated.
///
///   2. Version token bump
///      Replaces the version sentinel for a group+scope with a new timestamp.
///      This orphans every versioned list/search entry in that group without
///      any key scanning — O(1) regardless of how many filter combinations are cached.
///      The orphaned entries are never explicitly deleted; they simply miss on the
///      next read (wrong version in the key) and expire naturally.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(
    ICacheService cache,
    ICurrentTenantProvider tenantProvider,
    ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger,
    TimeProvider timeProvider)

    : IPipelineBehavior<TRequest, TResponse> where TRequest
    : IRequest<TResponse>, ICacheInvalidatorRequest

{
    // Version tokens bump to a 24 h TTL — same as CachingBehavior creates them.
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(1);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ArgumentNullException.ThrowIfNull(next);

        // Execute the command first — invalidate only on success.
        var response = await next(cancellationToken).ConfigureAwait(false);

        // ── 1. Direct key deletions ────────────────────────────────────────────
        foreach (var key in request.DirectInvalidationKeys)
        {
            await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);

            // CachingBehavior stores entity keys under a tenant-scoped variant for
            // tenant requests: "{group}:entity:tenant:{tenantId}:{discriminator}".
            // We must also remove that variant here so mutations don't leave stale
            // tenant-scoped entries in the cache.
            var tenantId = tenantProvider.TenantId;
            if (tenantId != Guid.Empty && key.Contains(":entity:", StringComparison.OrdinalIgnoreCase))
            {
                var tenantScopedKey = key.Replace(
                    ":entity:",
                    $":entity:tenant:{tenantId:D}:",
                    StringComparison.OrdinalIgnoreCase);
                await cache.RemoveAsync(tenantScopedKey, cancellationToken).ConfigureAwait(false);
            }
        }

        // ── 2. Version token bumps ─────────────────────────────────────────────
        foreach (var sentinelKey in request.GroupVersionKeysToInvalidate)
        {
            await BumpVersionAsync(sentinelKey, cancellationToken).ConfigureAwait(false);

            var tenantScopedKey = ToTenantScopedVersionKey(sentinelKey, tenantProvider.TenantId);
            if (!string.Equals(tenantScopedKey, sentinelKey, StringComparison.OrdinalIgnoreCase))
            {
                await BumpVersionAsync(tenantScopedKey, cancellationToken).ConfigureAwait(false);
            }
        }

        return response;
    }

    private async Task BumpVersionAsync(string sentinelKey, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(sentinelKey, cancellationToken).ConfigureAwait(false);
        CacheLogDefinitions.LogCacheBumped(logger, sentinelKey, GenerateVersion());
    }

    private static string ToTenantScopedVersionKey(string sentinelKey, Guid tenantId)
    {
        if (tenantId == Guid.Empty || !sentinelKey.EndsWith(":version:global", StringComparison.OrdinalIgnoreCase))
        {
            return sentinelKey;
        }

        return $"{sentinelKey[..^":global".Length]}:tenant:{tenantId:D}";
    }

    private string GenerateVersion()
        => timeProvider.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

}
