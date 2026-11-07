using ST.Domain.Shared.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace ST.Persistence.Features.ControlPlane.DataContext;

public class ControlPlanePostgreSqlDBContext : ControlPlaneDBContext
{
    public ControlPlanePostgreSqlDBContext(
        DbContextOptions<ControlPlanePostgreSqlDBContext> options,
        TimeProvider timeProvider,
        ICurrentActorProvider? actorProvider = null,
        ILogger<ControlPlanePostgreSqlDBContext>? logger = null)
        : base(options, timeProvider, actorProvider ?? new SystemActorProvider(), logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ControlPlanePostgreSqlDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.ControlPlane.EntityConfigurations.PostgreSql", StringComparison.Ordinal) == true);
    }

    private class SystemActorProvider : ICurrentActorProvider
    {
        public string ActorId => ICurrentActorProvider.SystemActor;
    }
}
