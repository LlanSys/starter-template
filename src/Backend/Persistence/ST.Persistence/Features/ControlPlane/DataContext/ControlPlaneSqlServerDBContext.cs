using ST.Domain.Shared.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace ST.Persistence.Features.ControlPlane.DataContext;

public class ControlPlaneSqlServerDBContext : ControlPlaneDBContext
{
    public ControlPlaneSqlServerDBContext(
        DbContextOptions<ControlPlaneSqlServerDBContext> options,
        TimeProvider timeProvider,
        ICurrentActorProvider? actorProvider = null,
        ILogger<ControlPlaneSqlServerDBContext>? logger = null)
        : base(options, timeProvider, actorProvider ?? new SystemActorProvider(), logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ControlPlaneSqlServerDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.ControlPlane.EntityConfigurations.SqlServer", StringComparison.Ordinal) == true);
    }

    private class SystemActorProvider : ICurrentActorProvider
    {
        public string ActorId => ICurrentActorProvider.SystemActor;
    }
}
