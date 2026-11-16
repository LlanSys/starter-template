using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class KycRetentionPolicyConfiguration : IEntityTypeConfiguration<KycRetentionPolicy>
{
    public void Configure(EntityTypeBuilder<KycRetentionPolicy> b)
    {
        b.ToTable("KycRetentionPolicies"); b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(80); b.Property(x => x.PublishedBy).HasMaxLength(128); b.Property(x => x.Reason).HasMaxLength(1000);
        b.HasData(new KycRetentionPolicy { Id = KycRetentionPolicy.DefaultId, PublishedAt = new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero), PublishedBy = "00000000-0000-0000-0000-000000000001", Reason = "Initial general-business retention schedule." });
    }
}
