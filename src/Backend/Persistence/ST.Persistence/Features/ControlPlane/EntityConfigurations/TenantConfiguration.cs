using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.Property(x => x.AdministratorUserId).HasMaxLength(128);
        builder.Property(x => x.ClientOrigin).HasMaxLength(2048);
        builder.Property(x => x.AdministratorEmail).HasMaxLength(256);
        builder.Property(x => x.KycApprovedBy).HasMaxLength(128);
        builder.Property(x => x.KycEvidenceReference).HasMaxLength(256);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Identifier)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.HostName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(256);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.SubscriptionTier)
            .IsRequired();

        builder.HasOne(x => x.DeploymentStamp)
            .WithMany()
            .HasForeignKey(x => x.DeploymentStampId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Modules)
            .WithOne(m => m.Tenant)
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(128);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ExclusiveStampId).IsUnique();
        builder.HasIndex(x => x.HostName).IsUnique();
        builder.HasIndex(x => x.Identifier).IsUnique();

        // Seed default tenant using the DefaultTenantId
        builder.HasData(new Tenant
        {
            Id = Guid.Parse("0194f700-0000-7000-8000-000000000001"),
            Identifier = "default",
            DisplayName = "Default Tenant",
            HostName = "localhost",
            ContactEmail = "admin@startertemplate.local",
            MaxUsers = 100,
            SubscriptionTier = SubscriptionTier.Free,
            Status = TenantStatus.Active,
            DeploymentStampId = Guid.Parse("0194f700-0000-7000-8000-000000000001"),
            CreatedBy = "System",
            CreatedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
