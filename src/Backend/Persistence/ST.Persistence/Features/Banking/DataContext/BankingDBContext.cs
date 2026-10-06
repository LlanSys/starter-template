using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Entities;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Domain.Shared.Entities;
using ST.Persistence.Common;
using ST.Persistence.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.Banking.DataContext;

public class BankingDBContext : DbContext, ITenantFilteredDBContext
{
    private readonly ICurrentTenantProvider? _tenantProvider;
    private readonly ICurrentActorProvider? _actorProvider;
    private readonly ILogger<BankingDBContext>? _logger;

    public BankingDBContext(
        DbContextOptions<BankingDBContext> options,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<BankingDBContext>? logger = null
    ) : base(options)
    {
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _logger = logger;
    }

    protected BankingDBContext(
        DbContextOptions options,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<BankingDBContext>? logger = null
    ) : base(options)
    {
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _logger = logger;
    }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Director> Directors { get; set; }
    public Guid CurrentTenantId => _tenantProvider?.TenantId ?? Guid.Empty;

    private List<IDomainEvent> _collectedDomainEvents = [];
    public IReadOnlyList<IDomainEvent>? GetCollectedDomainEvents() => _collectedDomainEvents?.AsReadOnly();
    public void ClearCollectedDomainEvents() => _collectedDomainEvents?.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BankingDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.Banking", StringComparison.Ordinal) == true &&
                    !(type.Namespace?.Contains("SqlServer") == true) &&
                    !(type.Namespace?.Contains("PostgreSql") == true));

        modelBuilder.Entity<Employee>()
            .ToTable("Employees", table => table.ExcludeFromMigrations());

        DBContextHelper.ApplyStandardModelConventions(modelBuilder, this);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var domainEvents = DBContextHelper.CollectDomainEvents(ChangeTracker);
            DBContextHelper.ClearDomainEventsFromAggregates(ChangeTracker);
            DBContextHelper.UpdateAuditAndSoftDelete(ChangeTracker, _actorProvider?.ActorId ?? ICurrentActorProvider.SystemActor, CurrentTenantId);
            var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _collectedDomainEvents ??= [];
            _collectedDomainEvents.AddRange(domainEvents);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                var entityId = entry.Entity is BaseEntity b ? b.Id.ToString() : "(unknown)";
                if (_logger is not null)
                    PersistenceLogDefinitions.LogConcurrencyConflict(_logger, entry.Entity.GetType().Name, entityId);
                _ = await entry.GetDatabaseValuesAsync(cancellationToken).ConfigureAwait(false);
            }
            _collectedDomainEvents?.Clear();
            throw;
        }
        catch (DbUpdateException ex)
        {
            foreach (var entry in ex.Entries)
                if (_logger is not null)
                    PersistenceLogDefinitions.LogDatabaseError(_logger, entry.Entity.GetType().Name, ex);
            _collectedDomainEvents?.Clear();
            throw;
        }
        catch (Exception ex)
        {
            if (_logger is not null)
                PersistenceLogDefinitions.LogDBContextSaveChangesError(_logger, nameof(BankingDBContext), ex);
            _collectedDomainEvents?.Clear();
            throw;
        }
    }
}
