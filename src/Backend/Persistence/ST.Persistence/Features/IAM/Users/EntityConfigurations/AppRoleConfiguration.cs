using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AspNetRoles");

        builder.Property(role => role.DepartmentId);

        builder.Property(role => role.DeletedBy)
            .HasMaxLength(100);

        builder.HasIndex(role => role.IsDeleted)
            .HasDatabaseName("IX_AspNetRoles_IsDeleted");

        builder.HasIndex(role => role.DepartmentId)
            .HasDatabaseName("IX_AspNetRoles_DepartmentId");
    }
}
