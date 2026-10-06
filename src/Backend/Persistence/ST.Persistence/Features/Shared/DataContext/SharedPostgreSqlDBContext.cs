using ST.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.Shared.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing PostgreSQL migrations.
/// </summary>
public class SharedPostgreSqlDBContext : SharedDBContext
{
    public SharedPostgreSqlDBContext(
        DbContextOptions<SharedPostgreSqlDBContext> options,
        ICurrentTenantProvider tenantProvider,
        ICurrentActorProvider actorProvider,
        ILogger<SharedDBContext>? logger = null)
        : base(options, tenantProvider, actorProvider, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SharedPostgreSqlDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.Shared.EntityConfigurations.PostgreSql", StringComparison.Ordinal) == true);
    }
}
