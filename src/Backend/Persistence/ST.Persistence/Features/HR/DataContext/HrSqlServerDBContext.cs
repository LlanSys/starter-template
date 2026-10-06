using ST.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.HR.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing SQL Server migrations.
/// </summary>
public class HrSqlServerDBContext : HrDBContext
{
    public HrSqlServerDBContext(
        DbContextOptions<HrSqlServerDBContext> options,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<HrDBContext>? logger = null)
        : base(options, tenantProvider, actorProvider, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HrSqlServerDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.HR.EntityConfigurations.SqlServer", StringComparison.Ordinal) == true);
    }
}
