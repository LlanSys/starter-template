using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.IAM.Menus.Entities;
namespace ST.Persistence.Features.IAM.Menus.EntityConfigurations;
internal sealed class NavigationOverrideConfiguration : IEntityTypeConfiguration<NavigationOverride>
{
    public void Configure(EntityTypeBuilder<NavigationOverride> b)
    {
        b.ToTable("NavigationOverrides"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.TenantId, x.PageKey }).IsUnique();
        b.Property(x => x.DeletedBy).HasMaxLength(128);
        b.Property(x => x.IsDeleted).HasDefaultValue(false);
        b.Property(x => x.PageKey).HasMaxLength(160); b.Property(x => x.Title).HasMaxLength(120);
        b.Property(x => x.Icon).HasMaxLength(80); b.Property(x => x.Placement).HasMaxLength(40);
        b.PrimitiveCollection(x => x.RoleNames).IsRequired(); b.PrimitiveCollection(x => x.PermissionKeys).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(128); b.Property(x => x.UpdatedBy).HasMaxLength(128);
    }
}
