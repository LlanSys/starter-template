using System;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class TenantConnectionResolver : ITenantConnectionResolver
{
    private readonly ICurrentTenantProvider _tenantProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEncryptionService _encryptionService;
    private readonly IMemoryCache _memoryCache;

    public TenantConnectionResolver(
        ICurrentTenantProvider tenantProvider,
        IServiceProvider serviceProvider,
        IEncryptionService encryptionService,
        IMemoryCache memoryCache)
    {
        _tenantProvider = tenantProvider;
        _serviceProvider = serviceProvider;
        _encryptionService = encryptionService;
        _memoryCache = memoryCache;
    }

    public async Task<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var tenantMetadata = await GetTenantMetadataAsync(cancellationToken).ConfigureAwait(false);
        return tenantMetadata?.ConnectionString;
    }

    public async Task<string?> GetDatabaseProviderAsync(CancellationToken cancellationToken = default)
    {
        var tenantMetadata = await GetTenantMetadataAsync(cancellationToken).ConfigureAwait(false);
        return tenantMetadata?.Provider;
    }

    private async Task<TenantDatabaseMetadata?> GetTenantMetadataAsync(CancellationToken cancellationToken)
    {
        Guid tenantId;
        try
        {
            tenantId = _tenantProvider.TenantId;
        }
        catch (TenantNotResolvedException)
        {
            // No tenant resolved for this request — pooled/anonymous path; return null so
            // TenantConnectionInterceptor leaves the default connection string untouched.
            return null;
        }

        var cacheKey = $"TenantDbMetadata_{tenantId}";
        if (_memoryCache.TryGetValue(cacheKey, out TenantDatabaseMetadata? cachedMetadata))
        {
            return cachedMetadata;
        }

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();

        var tenant = await unitOfWork.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant == null)
        {
            return null;
        }

        string? resolvedConnectionString = null;
        string? resolvedProvider = null;

        if (!string.IsNullOrEmpty(tenant.DatabaseConnectionString))
        {
            resolvedConnectionString = _encryptionService.Decrypt(tenant.DatabaseConnectionString);
            resolvedProvider = tenant.DatabaseProvider;
        }
        else
        {
            var stamp = await unitOfWork.DeploymentStamps.FindByIdAsync(tenant.DeploymentStampId, cancellationToken).ConfigureAwait(false);
            if (stamp != null && !string.IsNullOrEmpty(stamp.DatabaseConnectionString))
            {
                resolvedConnectionString = _encryptionService.Decrypt(stamp.DatabaseConnectionString);
                resolvedProvider = stamp.DatabaseProvider;
            }
        }

        var metadata = new TenantDatabaseMetadata(resolvedConnectionString, resolvedProvider);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

        _memoryCache.Set(cacheKey, metadata, cacheOptions);

        return metadata;
    }

    private record TenantDatabaseMetadata(string? ConnectionString, string? Provider);
}
