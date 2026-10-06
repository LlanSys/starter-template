using ST.Domain.Features.ControlPlane.Auditing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class ImpersonationRecordConfiguration : IEntityTypeConfiguration<ImpersonationRecord>
{
    public void Configure(EntityTypeBuilder<ImpersonationRecord> builder)
    {
        builder.ToTable("ImpersonationRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActorId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ActorName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.TargetTenantId)
            .IsRequired();

        builder.Property(x => x.TargetTenantName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(256);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Index on ActorId for finding an actor's impersonation history
        builder.HasIndex(x => x.ActorId);

        // Index on TargetTenantId for finding who impersonated a specific tenant
        builder.HasIndex(x => x.TargetTenantId);
    }
}
