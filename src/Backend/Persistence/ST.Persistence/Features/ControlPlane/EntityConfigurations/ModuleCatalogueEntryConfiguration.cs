using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Modules.Entities;
namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;
internal sealed class ModuleCatalogueEntryConfiguration : IEntityTypeConfiguration<ModuleCatalogueEntry>
{
    public void Configure(EntityTypeBuilder<ModuleCatalogueEntry> b)
    {
        b.ToTable("ModuleCatalogue"); b.HasKey(x => x.Id);
        b.HasIndex(x => x.Key).IsUnique(); b.HasIndex(x => new { x.ModuleKey, x.Kind });
        b.Property(x => x.Key).HasMaxLength(160); b.Property(x => x.ModuleKey).HasMaxLength(80);
        b.Property(x => x.Title).HasMaxLength(120); b.Property(x => x.Path).HasMaxLength(240);
        b.Property(x => x.ParentKey).HasMaxLength(160); b.Property(x => x.Icon).HasMaxLength(80);
        b.Property(x => x.Placement).HasMaxLength(40); b.Property(x => x.FeatureFlag).HasMaxLength(120);
        b.PrimitiveCollection(x => x.PermissionKeys).IsRequired(); b.PrimitiveCollection(x => x.RoleNames).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(128); b.Property(x => x.UpdatedBy).HasMaxLength(128);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasData(ModuleCatalogueSeed.Entries);
    }
}
