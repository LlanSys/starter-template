using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Exceptions;
using ST.Persistence.Common.Configuration;
using ST.Infrastructure.Configuration;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class TenantConnectionResolver(
    ICurrentTenantProvider tenantProvider,
    IServiceScopeFactory scopes,
    IEncryptionService encryption,
    IOptions<DatabaseSettings> databaseSettings,
    IOptions<OrgSettings> hostSettings) : ITenantConnectionResolver
{
    public async Task<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var metadata = await GetTenantMetadataAsync(cancellationToken).ConfigureAwait(false);
        return metadata?.ConnectionString;
    }

    public async Task<string?> GetDatabaseProviderAsync(CancellationToken cancellationToken = default)
    {
        var metadata = await GetTenantMetadataAsync(cancellationToken).ConfigureAwait(false);
        return metadata?.Provider;
    }

    private Task<TenantDatabaseMetadata?> GetTenantMetadataAsync(CancellationToken cancellationToken)
    {
        Guid tenantId;
        try
        {
            tenantId = tenantProvider.TenantId;
        }
        catch (TenantNotResolvedException)
        {
            return Task.FromResult<TenantDatabaseMetadata?>(null);
        }

        return LoadTenantMetadataAsync(tenantId, cancellationToken);
    }

    private async Task<TenantDatabaseMetadata?> LoadTenantMetadataAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var controlPlane = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();
        var tenant = await controlPlane.Tenants.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null || tenant.Status != TenantStatus.Active)
        {
            throw new TenantNotResolvedException("The requested workspace is unavailable.");
        }

        var stamp = tenant.DeploymentStamp
            ?? await controlPlane.DeploymentStamps.FindByIdAsync(tenant.DeploymentStampId, cancellationToken).ConfigureAwait(false);
        var connection = tenant.DatabaseConnectionString ?? stamp?.DatabaseConnectionString;
        var boundStamp = hostSettings.Value.DeploymentStampId;
        if (stamp is null || (boundStamp.HasValue && stamp.Id != boundStamp.Value)
            || (!boundStamp.HasValue && stamp.IsolationTier == IsolationTier.Isolated && string.IsNullOrWhiteSpace(connection)))
        {
            throw new TenantNotResolvedException("The requested workspace is unavailable.");
        }

        var provider = tenant.DatabaseProvider ?? stamp.DatabaseProvider;
        if (!string.IsNullOrWhiteSpace(provider)
            && !string.Equals(provider, databaseSettings.Value.Provider, StringComparison.OrdinalIgnoreCase))
        {
            throw new TenantNotResolvedException("The requested workspace requires a different database host.");
        }

        return new TenantDatabaseMetadata(boundStamp.HasValue || string.IsNullOrWhiteSpace(connection)
            ? null : encryption.Decrypt(connection), provider);
    }

    private sealed record TenantDatabaseMetadata(string? ConnectionString, string? Provider);
}
