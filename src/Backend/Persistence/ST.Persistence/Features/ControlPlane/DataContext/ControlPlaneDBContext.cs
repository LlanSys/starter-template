using ST.Domain.Features.ControlPlane.Auditing.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Common;
using ST.Persistence.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Features.ControlPlane.Support.Entities;

namespace ST.Persistence.Features.ControlPlane.DataContext;

public class ControlPlaneDBContext : DbContext
{
    private readonly ICurrentActorProvider? _actorProvider;
    private readonly ILogger<ControlPlaneDBContext>? _logger;
    internal TimeProvider Clock { get; }

    public ControlPlaneDBContext(
        DbContextOptions<ControlPlaneDBContext> options,
        TimeProvider timeProvider,
        ICurrentActorProvider actorProvider,
        ILogger<ControlPlaneDBContext>? logger = null
    ) : base(options)
    {
        _actorProvider = actorProvider;
        _logger = logger;
        Clock = timeProvider;
    }

    protected ControlPlaneDBContext(
        DbContextOptions options,
        TimeProvider timeProvider,
        ICurrentActorProvider actorProvider,
        ILogger<ControlPlaneDBContext>? logger = null
    ) : base(options)
    {
        _actorProvider = actorProvider;
        _logger = logger;
        Clock = timeProvider;
    }

    public DbSet<ModuleDefinition> ModuleDefinitions { get; set; }
    public DbSet<ModuleCatalogueEntry> ModuleCatalogue { get; set; }
    public DbSet<ModuleFeatureFlag> ModuleFeatureFlags { get; set; }
    public DbSet<SupportGrant> SupportGrants { get; set; }
    public DbSet<SupportAudit> SupportAudit { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantAdministratorInvitation> AdministratorInvitations { get; set; }
    public DbSet<TenantModule> TenantModules { get; set; }
    public DbSet<DeploymentStamp> DeploymentStamps { get; set; }
    public DbSet<ImpersonationRecord> ImpersonationRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ControlPlaneDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.ControlPlane", StringComparison.Ordinal) == true &&
                    !(type.Namespace?.Contains("SqlServer") == true) &&
                    !(type.Namespace?.Contains("PostgreSql") == true));
        if (Database.IsNpgsql()) DBContextHelper.ConfigurePostgreSqlConcurrency(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: Control Plane is not tenant-filtered. TenantId is Guid.Empty.
            DBContextHelper.StampPostgreSqlConcurrency(this);
            DBContextHelper.UpdateAuditAndSoftDelete(ChangeTracker, _actorProvider?.ActorId ?? ICurrentActorProvider.SystemActor, Guid.Empty, Clock.GetUtcNow());
            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                var entityId = entry.Property("Id").CurrentValue?.ToString() ?? "(unknown)";
                if (_logger is not null)
                    PersistenceLogDefinitions.LogConcurrencyConflict(_logger, entry.Entity.GetType().Name, entityId);
                _ = await entry.GetDatabaseValuesAsync(cancellationToken).ConfigureAwait(false);
            }
            throw;
        }
        catch (DbUpdateException ex)
        {
            foreach (var entry in ex.Entries)
                if (_logger is not null)
                    PersistenceLogDefinitions.LogDatabaseError(_logger, entry.Entity.GetType().Name, ex);
            throw;
        }
        catch (Exception ex)
        {
            if (_logger is not null)
                PersistenceLogDefinitions.LogDBContextSaveChangesError(_logger, nameof(ControlPlaneDBContext), ex);
            throw;
        }
    }
}
