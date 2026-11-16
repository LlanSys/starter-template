using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class KycSubmissionLinkConfiguration : IEntityTypeConfiguration<KycSubmissionLink>
{
    public void Configure(EntityTypeBuilder<KycSubmissionLink> builder)
    {
        builder.ToTable("KycSubmissionLinks");
        builder.Ignore(item => item.SubmitterId);
        builder.HasKey(item => item.TenantId);
        builder.HasOne<Tenant>().WithOne().HasForeignKey<KycSubmissionLink>(item => item.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.TokenHash).HasMaxLength(64).IsFixedLength();
        builder.Property(item => item.IssuedBy).HasMaxLength(128);
        builder.Property(item => item.Revision).IsConcurrencyToken();
    }
}
