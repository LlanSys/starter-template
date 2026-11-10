using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ST.Domain.Features.IAM.Users.Entities;

namespace ST.Persistence.Features.IAM.DataContext;

internal static class IdentityTenantWriteBoundary
{
    public static async Task EnforceAsync(IamDBContext context, Guid tenantId, CancellationToken cancellationToken)
    {
        var userIds = new HashSet<string>(StringComparer.Ordinal);
        var roleIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in context.ChangeTracker.Entries().Where(entry =>
            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            foreach (var foreignKey in entry.Metadata.GetForeignKeys())
            {
                var principal = foreignKey.PrincipalEntityType.ClrType;
                if (principal == typeof(AppUser))
                {
                    Collect(entry, foreignKey.Properties[0].Name, userIds);
                }
                else if (principal == typeof(AppRole))
                {
                    Collect(entry, foreignKey.Properties[0].Name, roleIds);
                }
            }
        }

        if (userIds.Count > 0)
        {
            foreach (var entry in context.ChangeTracker.Entries<AppUser>()
                .Where(entry => entry.State == EntityState.Added && entry.Entity.TenantId == tenantId))
            {
                userIds.Remove(entry.Entity.Id);
            }

            var found = await context.Users.IgnoreQueryFilters().CountAsync(user =>
                userIds.Contains(user.Id) && user.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
            if (found != userIds.Count)
            {
                throw new InvalidOperationException("An identity write must target a user in the current workspace.");
            }
        }

        if (roleIds.Count > 0)
        {
            foreach (var entry in context.ChangeTracker.Entries<AppRole>()
                .Where(entry => entry.State == EntityState.Added && entry.Entity.TenantId == tenantId))
            {
                roleIds.Remove(entry.Entity.Id);
            }

            var found = await context.Roles.IgnoreQueryFilters().CountAsync(role =>
                roleIds.Contains(role.Id) && role.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
            if (found != roleIds.Count)
            {
                throw new InvalidOperationException("An identity write must target a role in the current workspace.");
            }
        }
    }

    private static void Collect(EntityEntry entry, string propertyName, HashSet<string> ids)
    {
        var property = entry.Property(propertyName);
        if (property.CurrentValue is string current)
        {
            ids.Add(current);
        }

        if (entry.State != EntityState.Added && property.OriginalValue is string original)
        {
            ids.Add(original);
        }
    }
}
