using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Features.IAM.Permissions.Entities;
using ST.Domain.Features.IAM.ReferenceData.Entities;
using ST.Domain.Shared.Entities;
using ST.Persistence.Common;
using ST.Persistence.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ST.Persistence.Features.IAM.DataContext;

public class IamDBContext : IdentityDbContext<AppUser, AppRole, string>, ITenantFilteredDBContext
{
    private readonly ICurrentTenantProvider? _tenantProvider;
    private readonly ICurrentActorProvider? _actorProvider;
    private readonly ILogger<IamDBContext>? _logger;
    internal TimeProvider Clock { get; }

    public IamDBContext(
        DbContextOptions<IamDBContext> options,
        TimeProvider timeProvider,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<IamDBContext>? logger = null
    ) : base(options)
    {
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _logger = logger;
        Clock = timeProvider;
    }

    protected IamDBContext(
        DbContextOptions options,
        TimeProvider timeProvider,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<IamDBContext>? logger = null
    ) : base(options)
    {
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _logger = logger;
        Clock = timeProvider;
    }
    public DbSet<AppUserProfile> AppUserProfiles { get; set; }
    public DbSet<AppUserTotpSecret> AppUserTotpSecrets { get; set; }
    public DbSet<AppUserSession> AppUserSessions { get; set; }
    public DbSet<AppUserDevice> AppUserDevices { get; set; }
    public DbSet<PasskeyChallenge> PasskeyChallenges { get; set; }
    public DbSet<Fido2Credential> Fido2Credentials { get; set; }
    public DbSet<TempTotpSecret> TempTotpSecrets { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<SsoAuthorizationCode> SsoAuthorizationCodes { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<PermissionContext> PermissionContexts { get; set; }
    public DbSet<PermissionResource> PermissionResources { get; set; }
    public DbSet<PermissionAction> PermissionActions { get; set; }
    public DbSet<MenuPlacement> MenuPlacements { get; set; }
    public DbSet<MenuIcon> MenuIcons { get; set; }
    public DbSet<MenuRoute> MenuRoutes { get; set; }
    public DbSet<NavigationOverride> NavigationOverrides { get; set; }

    public Guid CurrentTenantId => _tenantProvider?.TenantId ?? Guid.Empty;

    private List<IDomainEvent> _collectedDomainEvents = [];
    public IReadOnlyList<IDomainEvent>? GetCollectedDomainEvents() => _collectedDomainEvents?.AsReadOnly();
    public void ClearCollectedDomainEvents() => _collectedDomainEvents?.Clear();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(IamDBContext).Assembly,
            type => type.Namespace?.StartsWith("ST.Persistence.Features.IAM", StringComparison.Ordinal) == true &&
                    !(type.Namespace?.Contains("SqlServer") == true) &&
                    !(type.Namespace?.Contains("PostgreSql") == true));

        DBContextHelper.ApplyStandardModelConventions(builder, this);

        builder.Entity<AppUser>(entity => entity.ToTable("Users"));
        builder.Entity<AppRole>(entity => entity.ToTable("Roles"));
        builder.Entity<IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));
        builder.Entity<IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var domainEvents = DBContextHelper.CollectDomainEvents(ChangeTracker);
            DBContextHelper.ClearDomainEventsFromAggregates(ChangeTracker);
            DBContextHelper.StampPostgreSqlConcurrency(this);
            DBContextHelper.UpdateAuditAndSoftDelete(ChangeTracker, _actorProvider?.ActorId ?? ICurrentActorProvider.SystemActor, CurrentTenantId, Clock.GetUtcNow());

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
                PersistenceLogDefinitions.LogDBContextSaveChangesError(_logger, nameof(IamDBContext), ex);
            _collectedDomainEvents?.Clear();
            throw;
        }
    }
}
