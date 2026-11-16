using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class KycRetentionAuditConfiguration : IEntityTypeConfiguration<KycRetentionAudit>
{
    public void Configure(EntityTypeBuilder<KycRetentionAudit> b)
    {
        b.ToTable("KycRetentionAudit"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.TenantId, x.OccurredAt });
        b.Property(x => x.Action).HasMaxLength(40); b.Property(x => x.ActorId).HasMaxLength(128); b.Property(x => x.Reason).HasMaxLength(1000);
    }
}
