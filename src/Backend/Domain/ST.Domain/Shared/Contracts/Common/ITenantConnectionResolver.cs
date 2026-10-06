using System.Threading;
using System.Threading.Tasks;

namespace ST.Domain.Shared.Contracts.Common;

/// <summary>
/// Resolves the database connection string and provider for the current tenant.
/// </summary>
public interface ITenantConnectionResolver
{
    /// <summary>
    /// Gets the resolved connection string for the current tenant.
    /// Returns null if the default connection should be used.
    /// </summary>
    Task<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the resolved database provider name for the current tenant.
    /// Returns null if the default provider should be used.
    /// </summary>
    Task<string?> GetDatabaseProviderAsync(CancellationToken cancellationToken = default);
}
