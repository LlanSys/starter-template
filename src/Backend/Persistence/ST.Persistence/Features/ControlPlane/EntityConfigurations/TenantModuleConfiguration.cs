using ST.Domain.Features.ControlPlane.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.ControlPlane.EntityConfigurations;

internal class TenantModuleConfiguration : IEntityTypeConfiguration<TenantModule>
{
    public void Configure(EntityTypeBuilder<TenantModule> builder)
    {
        builder.ToTable("TenantModules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModuleKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(128);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => new { x.TenantId, x.ModuleKey }).IsUnique();

        var defaultTenantId = Guid.Parse("0194f700-0000-7000-8000-000000000001");
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            CreateSeedModule(Guid.Parse("0194f700-0000-7000-8000-000000000002"), defaultTenantId, "Core", createdAt),
            CreateSeedModule(Guid.Parse("0194f700-0000-7000-8000-000000000003"), defaultTenantId, "IAM", createdAt),
            CreateSeedModule(Guid.Parse("0194f700-0000-7000-8000-000000000004"), defaultTenantId, "Banking", createdAt),
            CreateSeedModule(Guid.Parse("0194f700-0000-7000-8000-000000000005"), defaultTenantId, "HR", createdAt)
        );
    }

    private static TenantModule CreateSeedModule(Guid id, Guid tenantId, string moduleKey, DateTimeOffset createdAt)
    {
        return new TenantModule
        {
            Id = id,
            TenantId = tenantId,
            ModuleKey = moduleKey,
            CreatedBy = "System",
            CreatedAt = createdAt,
            IsActive = true
        };
    }
}
