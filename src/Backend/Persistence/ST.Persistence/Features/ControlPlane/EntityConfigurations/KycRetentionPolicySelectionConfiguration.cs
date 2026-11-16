using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class KycRetentionPolicySelectionConfiguration : IEntityTypeConfiguration<KycRetentionPolicySelection>
{
    public void Configure(EntityTypeBuilder<KycRetentionPolicySelection> b)
    {
        b.ToTable("KycRetentionPolicySelection"); b.HasKey(x => x.Id); b.Property(x => x.Revision).IsConcurrencyToken();
        b.HasOne<KycRetentionPolicy>().WithMany().HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.Restrict);
        b.HasData(new KycRetentionPolicySelection());
    }
}
