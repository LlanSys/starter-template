using ST.Domain.Features.Shared.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.Shared.Payments.EntityConfigurations;

internal sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("PaymentRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CustomerReference).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.CustomerReference).IsUnique();

        builder.Property(x => x.Provider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProviderReference).HasMaxLength(200);
        builder.Property(x => x.StatusMessage).HasMaxLength(1000);
        builder.HasIndex(x => x.ProviderReference).HasFilter("[ProviderReference] IS NOT NULL");
        builder.Property(x => x.IdempotencyKey).HasMaxLength(100);
        builder.Property(x => x.CheckoutUrl).HasMaxLength(2048);
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.OwnsOne(x => x.Amount, amountBuilder =>
        {
            amountBuilder.Property(a => a.Amount)
                .HasColumnName("Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            amountBuilder.Property(a => a.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
