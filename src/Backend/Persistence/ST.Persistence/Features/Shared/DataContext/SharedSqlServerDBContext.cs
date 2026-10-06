using Microsoft.EntityFrameworkCore;
using ST.Domain.Shared.Contracts.Common;

namespace ST.Persistence.Features.Shared.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing SQL Server migrations.
/// </summary>
public class SharedSqlServerDBContext : SharedDBContext
{
    public SharedSqlServerDBContext(
        DbContextOptions<SharedSqlServerDBContext> options,
        ICurrentTenantProvider tenantProvider,
        ICurrentActorProvider actorProvider,
        Microsoft.Extensions.Logging.ILogger<SharedDBContext>? logger = null)
        : base(options, tenantProvider, actorProvider, logger)
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
