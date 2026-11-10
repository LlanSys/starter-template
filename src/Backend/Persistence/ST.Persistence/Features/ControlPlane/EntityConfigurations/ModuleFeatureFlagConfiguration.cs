using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Modules.Entities;
namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;
internal sealed class ModuleFeatureFlagConfiguration : IEntityTypeConfiguration<ModuleFeatureFlag>
{
    public void Configure(EntityTypeBuilder<ModuleFeatureFlag> b)
    {
        b.ToTable("ModuleFeatureFlags"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ScopeTenantId, x.ModuleKey, x.Key }).IsUnique();
        b.Property(x => x.ModuleKey).HasMaxLength(80); b.Property(x => x.Key).HasMaxLength(120);
        b.Property(x => x.Name).HasMaxLength(120); b.Property(x => x.CreatedBy).HasMaxLength(128);
        b.Property(x => x.UpdatedBy).HasMaxLength(128); b.Property(x => x.RowVersion).IsRowVersion();
    }
}
