using ST.Domain.Features.Shared.OrgSettings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.Shared.OrgSettings.EntityConfigurations;

internal class OrgSettingConfiguration : IEntityTypeConfiguration<OrgSetting>
{
    public void Configure(EntityTypeBuilder<OrgSetting> builder)
    {
        builder.ToTable("OrgSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Value)
            .IsRequired(); // No max length, could be large JSON or encrypted string

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        // Ensure a tenant can only have one setting per key
        builder.HasIndex(x => new { x.TenantId, x.Key })
            .IsUnique();
    }
}
