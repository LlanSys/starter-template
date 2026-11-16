using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.Domain.Features.ControlPlane.Kyc.Entities;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal sealed class KycDocumentReviewConfiguration : IEntityTypeConfiguration<KycDocumentReview>
{
    public void Configure(EntityTypeBuilder<KycDocumentReview> builder)
    {
        builder.ToTable("KycDocumentReviews");
        builder.HasKey(item => item.Id);
        builder.HasOne<KycDocument>().WithMany().HasForeignKey(item => item.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.DocumentId, item.ReviewedAt });
        builder.Property(item => item.Feedback).HasMaxLength(2000);
        builder.Property(item => item.ReviewedBy).HasMaxLength(128);
    }
}
