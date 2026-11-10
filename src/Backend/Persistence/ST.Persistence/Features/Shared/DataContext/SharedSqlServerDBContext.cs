using Microsoft.EntityFrameworkCore;
using ST.Domain.Shared.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.Shared.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing SQL Server migrations.
/// </summary>
public class SharedSqlServerDBContext : SharedDBContext
{
    public SharedSqlServerDBContext(
        DbContextOptions<SharedSqlServerDBContext> options,
        TimeProvider timeProvider,
        ICurrentTenantProvider tenantProvider,
        ICurrentActorProvider actorProvider,
        ILogger<SharedDBContext>? logger = null)
        : base(options, timeProvider, tenantProvider, actorProvider, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SharedSqlServerDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.Shared.EntityConfigurations.SqlServer", StringComparison.Ordinal) == true);
    }
}
