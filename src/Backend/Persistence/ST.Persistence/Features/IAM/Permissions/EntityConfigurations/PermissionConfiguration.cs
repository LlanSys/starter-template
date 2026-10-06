using ST.Domain.Features.IAM.Permissions.Entities;
using ST.Persistence.Features.IAM.Permissions.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.IAM.Permissions.EntityConfigurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Key)
            .HasMaxLength(160)
            .IsRequired();

        builder.HasIndex(permission => permission.Key)
            .IsUnique()
            .HasDatabaseName("UX_Permissions_Key");

        builder.Property(permission => permission.Context)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(permission => permission.Resource)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(permission => permission.Action)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(permission => permission.Description)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(permission => permission.CreatedBy)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(permission => permission.UpdatedBy)
            .HasMaxLength(120);

        builder.Property(permission => permission.DeletedBy)
            .HasMaxLength(120);

        builder.HasData(PermissionSeed.Items);
    }
}
