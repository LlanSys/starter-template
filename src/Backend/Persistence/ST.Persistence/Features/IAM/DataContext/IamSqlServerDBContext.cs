using ST.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.IAM.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing SQL Server migrations.
/// </summary>
public class IamSqlServerDBContext : IamDBContext
{
    public IamSqlServerDBContext(
        DbContextOptions<IamSqlServerDBContext> options,
        TimeProvider timeProvider,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<IamDBContext>? logger = null)
        : base(options, timeProvider, tenantProvider, actorProvider, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(
            typeof(IamSqlServerDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.IAM.EntityConfigurations.SqlServer", StringComparison.Ordinal) == true);
    }
}
