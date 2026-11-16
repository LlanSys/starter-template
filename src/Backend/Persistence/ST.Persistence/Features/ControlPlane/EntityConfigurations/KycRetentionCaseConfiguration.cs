using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class KycRetentionCaseConfiguration : IEntityTypeConfiguration<KycRetentionCase>
{
    public void Configure(EntityTypeBuilder<KycRetentionCase> b)
    {
        b.ToTable("KycRetentionCases"); b.HasKey(x => x.TenantId); b.Property(x => x.Revision).IsConcurrencyToken();
        b.Property(x => x.Outcome).HasMaxLength(20); b.Property(x => x.Phase).HasMaxLength(20); b.Property(x => x.FailureCode).HasMaxLength(40);
        b.HasIndex(x => new { x.Phase, x.RetainUntil }); b.HasIndex(x => x.NextAttemptAt);
        b.HasOne<KycRetentionPolicy>().WithMany().HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(x => x.CanCollect);
    }
}
