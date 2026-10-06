using ST.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.IAM.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing PostgreSQL migrations.
/// </summary>
public class IamPostgreSqlDBContext : IamDBContext
{
    public IamPostgreSqlDBContext(
        DbContextOptions<IamPostgreSqlDBContext> options,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<IamDBContext>? logger = null)
        : base(options, tenantProvider, actorProvider, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(
            typeof(IamPostgreSqlDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.IAM.EntityConfigurations.PostgreSql", StringComparison.Ordinal) == true);
    }
}
