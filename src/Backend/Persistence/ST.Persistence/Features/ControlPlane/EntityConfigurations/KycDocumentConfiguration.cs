using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        builder.ToTable("KycDocuments");
        builder.HasKey(item => item.Id);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TenantId, item.Sha256, item.UploadedBy, item.DocumentTypeKey, item.DocumentType }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.DocumentTypeKey, item.UploadedAt });
        builder.Property(item => item.DocumentTypeKey).HasMaxLength(64).HasDefaultValue("legacy");
        builder.Property(item => item.DocumentType).HasMaxLength(80);
        builder.Property(item => item.FileName).HasMaxLength(180);
        builder.Property(item => item.ContentType).HasMaxLength(80);
        builder.Property(item => item.Sha256).HasMaxLength(64).IsFixedLength();
        builder.Property(item => item.UploadedBy).HasMaxLength(128);
        builder.Property(item => item.Status).HasMaxLength(32);
        builder.Property(item => item.Revision).IsConcurrencyToken();
        builder.Property(item => item.SafetyStatus).HasConversion<int>();
        builder.Property(item => item.SafetyEngine).HasMaxLength(128);
        builder.Property(item => item.SafetyDefinitions).HasMaxLength(64);
        builder.Property(item => item.SafetyReasonCode).HasMaxLength(64);
        builder.HasIndex(item => new { item.SafetyStatus, item.NextSafetyScanAt, item.SafetyLeaseUntil });
        builder.Ignore(item => item.StorageKey);
    }
}
