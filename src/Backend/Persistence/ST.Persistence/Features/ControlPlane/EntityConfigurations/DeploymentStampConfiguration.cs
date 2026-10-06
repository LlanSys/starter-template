using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal class DeploymentStampConfiguration : IEntityTypeConfiguration<DeploymentStamp>
{
    public void Configure(EntityTypeBuilder<DeploymentStamp> builder)
    {
        builder.ToTable("DeploymentStamps");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.TargetResourceGroup)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.IsolationTier)
            .IsRequired();

        builder.Property(x => x.KeyVaultUri)
            .HasMaxLength(1024);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(128);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.Name).IsUnique();

        // Seed default stamp
        builder.HasData(new DeploymentStamp
        {
            Id = Guid.Parse("0194f700-0000-7000-8000-000000000001"),
            Name = "default-pooled-stamp",
            TargetResourceGroup = "rg-startertemplate-dev",
            IsolationTier = IsolationTier.Pooled,
            CreatedBy = "System",
            CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
