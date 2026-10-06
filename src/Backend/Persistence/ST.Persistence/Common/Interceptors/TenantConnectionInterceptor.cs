using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using ST.Domain.Shared.Contracts.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Persistence.Common.Interceptors;

/// <summary>
/// Intercepts database connection opening to dynamically override the connection string
/// based on the current tenant's configured database connection.
/// </summary>
public class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public TenantConnectionInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        throw new NotSupportedException(
            "Synchronous connection opening is not supported with tenant-scoped connection resolution. " +
            "Use OpenAsync()/ExecuteAsync-based EF Core APIs so the correct tenant connection string can be resolved.");
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ITenantConnectionResolver>();

        var newConnectionString = await resolver.GetConnectionStringAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(newConnectionString) && connection.ConnectionString != newConnectionString)
        {
            connection.ConnectionString = newConnectionString;
        }

        return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken).ConfigureAwait(false);
    }
}
