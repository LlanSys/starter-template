using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.IAM.PlatformStaff.Entities;

namespace ST.Persistence.Features.IAM.PlatformStaff.EntityConfigurations;

internal sealed class PlatformStaffChangeConfiguration : IEntityTypeConfiguration<PlatformStaffChange>
{
    public void Configure(EntityTypeBuilder<PlatformStaffChange> builder)
    {
        builder.ToTable("PlatformStaffChanges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.ActorId).HasMaxLength(450);
        builder.Property(x => x.Action).HasMaxLength(60);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.PreviousPermissionsJson).HasMaxLength(6000);
        builder.Property(x => x.CurrentPermissionsJson).HasMaxLength(6000);
        builder.HasIndex(x => new { x.UserId, x.OccurredAt });
        builder.HasIndex(x => x.OccurredAt);
    // Immutable audit identifiers deliberately survive account retirement; no cascading user FK.
    }
}
