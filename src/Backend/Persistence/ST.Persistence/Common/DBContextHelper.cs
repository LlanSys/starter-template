using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace ST.Persistence.Common;

internal static class DBContextHelper
{
    public static void ApplyStandardModelConventions<TContext>(ModelBuilder modelBuilder, TContext context)
        where TContext : DbContext, ITenantFilteredDBContext
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null || entityType.IsOwned())
            {
                continue;
            }

            if (((DbContext)context).Database.IsNpgsql())
            {
                var rowVersionProp = entityType.FindProperty(nameof(BaseEntity.RowVersion));
                if (rowVersionProp != null)
                {
                    rowVersionProp.SetDefaultValue(Array.Empty<byte>());
                }

                foreach (var index in entityType.GetIndexes())
                {
                    var filter = index.GetFilter();
                    if (!string.IsNullOrEmpty(filter))
                    {
                        var newFilter = filter.Replace("[", "\"").Replace("]", "\"")
                            .Replace(" = 0", " = false").Replace(" = 1", " = true");
                        index.SetFilter(newFilter);
                    }
                }
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    property.SetProviderClrType(typeof(string));

                    if (property.GetMaxLength() == null)
                    {
                        property.SetMaxLength(100);
                    }
                }
            }

            ApplyQueryFilters(modelBuilder, entityType, context);
            ApplyCursorPagination(modelBuilder, entityType);
        }
    }

    public static void UpdateAuditAndSoftDelete(ChangeTracker changeTracker, string userId, Guid tenantId)
    {
        StampTenantIds(changeTracker, tenantId);

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in changeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.UpdatedAt = null;
                    entry.Entity.UpdatedBy = null;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;

                    if (entry.Entity is ISoftDeletable { IsDeleted: true, DeletedAt: null } softDeleted)
                    {
                        softDeleted.DeletedAt = now;
                        softDeleted.DeletedBy = userId;
                    }

                    break;
                case EntityState.Deleted:
                    if (entry.Entity is ISoftDeletable sd)
                    {
                        entry.State = EntityState.Modified;
                        sd.IsDeleted = true;
                        sd.DeletedAt = now;
                        sd.DeletedBy = userId;
                    }
                    break;
            }
        }
    }

    public static List<IDomainEvent> CollectDomainEvents(ChangeTracker changeTracker)
    {
        var events = new List<IDomainEvent>();
        foreach (var entry in changeTracker.Entries<IHasDomainEvents>())
            if (entry.Entity.DomainEvents.Any())
                events.AddRange(entry.Entity.DomainEvents);
        return events;
    }

    public static void ClearDomainEventsFromAggregates(ChangeTracker changeTracker)
    {
        foreach (var entry in changeTracker.Entries<IHasDomainEvents>())
            entry.Entity.ClearDomainEvents();
    }

    public static LambdaExpression CreateSoftDeleteFilter(Type type)
    {
        var parameter = Expression.Parameter(type, "e");
        var property = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(bool)],
            parameter,
            Expression.Constant(nameof(ISoftDeletable.IsDeleted)));
        var comparison = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(comparison, parameter);
    }

    private static void ApplyQueryFilters<TContext>(ModelBuilder modelBuilder, IMutableEntityType entityType, TContext context)
        where TContext : DbContext, ITenantFilteredDBContext
    {
        var entityClrType = entityType.ClrType;
#pragma warning disable CS0618 // ToDo: EF Core 10 obsoletes unnamed filters; keep composing existing filters until we migrate to named filters solution-wide.
        var queryFilter = entityType.GetQueryFilter();
#pragma warning restore CS0618

        if (typeof(ISoftDeletable).IsAssignableFrom(entityClrType))
        {
            queryFilter = CombineFilters(entityClrType, queryFilter, CreateSoftDeleteFilter(entityClrType));
        }

        if (HasTenantId(entityType))
        {
            var tenantFilter = (LambdaExpression)typeof(DBContextHelper)
                .GetMethod(nameof(CreateTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityClrType, typeof(TContext))
                .Invoke(null, [context])!;

            queryFilter = CombineFilters(entityClrType, queryFilter, tenantFilter);
        }

        if (queryFilter is not null)
        {
            modelBuilder.Entity(entityClrType).HasQueryFilter(queryFilter);
        }
    }

    private static void ApplyCursorPagination(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        if (!typeof(ICursorPaginable).IsAssignableFrom(entityType.ClrType))
        {
            return;
        }

        modelBuilder.Entity(entityType.ClrType).HasKey(nameof(ICursorPaginable.Id));
        modelBuilder.Entity(entityType.ClrType)
            .HasIndex(nameof(ICursorPaginable.CreatedAt), nameof(ICursorPaginable.Id))
            .HasDatabaseName($"IX_{entityType.GetTableName()}_CreatedAt_Id");
    }

    private static Expression<Func<TEntity, bool>> CreateTenantFilter<TEntity, TContext>(TContext context)
        where TEntity : class
        where TContext : DbContext, ITenantFilteredDBContext
    {
        return e => EF.Property<Guid>(e, nameof(BaseEntity.TenantId)) == context.CurrentTenantId;
    }

    private static LambdaExpression CombineFilters(Type type, LambdaExpression? left, LambdaExpression right)
    {
        if (left is null)
        {
            return right;
        }

        var parameter = Expression.Parameter(type, "e");
        var leftBody = new ReplaceExpressionVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ReplaceExpressionVisitor(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private static bool HasTenantId(IMutableEntityType entityType)
    {
        return entityType.FindProperty(nameof(BaseEntity.TenantId))?.ClrType == typeof(Guid);
    }

    private static void StampTenantIds(ChangeTracker changeTracker, Guid tenantId)
    {
        foreach (var entry in changeTracker.Entries().Where(static entry => entry.State == EntityState.Added))
        {
            var tenantProperty = entry.Properties.FirstOrDefault(static property =>
                property.Metadata.Name == nameof(BaseEntity.TenantId) &&
                property.Metadata.ClrType == typeof(Guid));

            if (tenantProperty?.CurrentValue is not Guid currentTenantId || currentTenantId != Guid.Empty)
            {
                continue;
            }

            if (tenantId == Guid.Empty)
            {
                throw new InvalidOperationException($"Cannot save tenant-scoped entity '{entry.Metadata.Name}' because no current tenant was resolved.");
            }

            tenantProperty.CurrentValue = tenantId;
        }
    }
}
