using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.Shared.OrgSettings.Entities;
namespace ST.Persistence.Features.Shared.OrgSettings.EntityConfigurations;
internal sealed class OrgSettingChangeConfiguration : IEntityTypeConfiguration<OrgSettingChange>
{
    public void Configure(EntityTypeBuilder<OrgSettingChange> builder)
    {
        builder.ToTable("OrgSettingChanges");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Key).HasMaxLength(100).IsRequired();
        builder.Property(change => change.PreviousValue).HasMaxLength(128);
        builder.Property(change => change.Value).HasMaxLength(128);
        builder.HasIndex(change => new { change.TenantId, change.CreatedAt });
    }
}
