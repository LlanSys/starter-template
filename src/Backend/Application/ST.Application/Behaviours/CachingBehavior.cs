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
/// MediatR pipeline behavior that provides transparent read-through caching
/// for any query implementing <see cref="ICachableRequest"/>.
///
/// Registration order matters — register this AFTER validation behaviors
/// so invalid requests are rejected before a cache lookup.
///
/// Key assembly:
///   Non-versioned  →  "{group}:entity:{discriminator}"
///   Versioned      →  "{group}:list:{scope}:{versionToken}:{discriminator}"
///
/// Version token lifecycle:
///   Created lazily on the first cache miss; stored with a long TTL (24 h).
///   Bumped (replaced) by <see cref="CacheInvalidationBehavior{TRequest,TResponse}"/>
///   whenever a mutation command succeeds, which orphans all versioned entries
///   in the group without any key scanning.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cache,
    ICurrentTenantProvider tenantProvider,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>, ICachableRequest
{
    // Version tokens live longer than the entries they version.
    // If a version token expires, the next request simply creates a new one —
    // effectively a full cache miss for the group, which is safe.
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(1);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // ── 1. Bypass check ────────────────────────────────────────────────────
        if (request.BypassCache)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        // ── 2. Key assembly ────────────────────────────────────────────────────
        string cacheKey;

        if (request.IsVersioned)
        {
            var scope = BuildTenantScope(tenantProvider.TenantId, request.CacheUserId);
            var sentinelKey = CacheKeys.GroupVersion(request.CacheGroup, scope);
            var versionToken = await ResolveOrCreateVersionAsync(sentinelKey, cancellationToken).ConfigureAwait(false);

            cacheKey = CacheKeys.VersionedList(
                request.CacheGroup,
                scope,
                versionToken,
                request.Discriminator);
        }
        else
        {
            // Entity (non-versioned) lookups are scoped per-tenant to prevent cross-tenant
            // cache leakage.  Platform admins (empty TenantId) get the bare global key.
            //
            // Key format for tenant requests: "{group}:entity:tenant:{tenantId}:{discriminator}"
            // Key format for platform admins:  "{group}:entity:{discriminator}"
            var tenantId = tenantProvider.TenantId;
            cacheKey = tenantId == Guid.Empty
                ? CacheKeys.Entity(request.CacheGroup, request.Discriminator)
                : CacheKeys.Entity(request.CacheGroup, $"tenant:{tenantId:D}:{request.Discriminator}");
        }

        // ── 3. Stampede-Protected Cache Lookup & Execution ─────────────────────
        var ttl = request.Expiration ?? TimeSpan.FromMinutes(30);

        var response = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                CacheLogDefinitions.LogCacheMiss(logger, cacheKey);
                return await next(ct).ConfigureAwait(false);
            },
            ttl,
            cancellationToken).ConfigureAwait(false);

        if (response is ST.SharedKernel.Dtos.Common.IAppResponse appResponse && !appResponse.IsSuccess)
        {
            // Do not cache failed responses (especially since errors don't serialize well).
            await cache.RemoveAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private async Task<string> ResolveOrCreateVersionAsync(string sentinelKey, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            sentinelKey,
            async token =>
            {
                var version = GenerateVersion();
                CacheLogDefinitions.LogCacheSet(logger, sentinelKey, VersionTtl);
                return version;
            },
            VersionTtl,
            ct).ConfigureAwait(false);
    }

    private static string GenerateVersion()
        => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

    private static string BuildTenantScope(Guid tenantId, string? userId)
    {
        var tenantScope = tenantId == Guid.Empty ? "tenant:unknown" : $"tenant:{tenantId:D}";
        return string.IsNullOrWhiteSpace(userId)
            ? tenantScope
            : $"{tenantScope}:user:{userId}";
    }
}
