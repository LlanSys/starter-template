using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.DeactivationReason)
            .HasMaxLength(500);

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.ActivatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.DeactivatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.DeletedBy)
            .HasMaxLength(100);

        builder.HasIndex(u => u.NormalizedUserName).IsUnique(false).HasDatabaseName("IX_AppUsers_NormalizedUserName");
        builder.HasIndex(u => new { u.TenantId, u.NormalizedUserName }).IsUnique().HasDatabaseName("UX_AppUsers_Tenant_UserName");
        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_AppUsers_TenantId");

        builder.HasIndex(u => new { u.TenantId, u.IsActive, u.IsDeleted })
            .HasDatabaseName("IX_AppUsers_TenantId_IsActive_IsDeleted");
    }
}
