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

        builder.Property(u => u.NationalId)
            .HasMaxLength(100);

        builder.Property(u => u.RegistrationNumber)
            .HasMaxLength(100);

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

        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_AppUsers_TenantId");

        builder.HasIndex(u => u.EmployeeId)
            .IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL")
            .HasDatabaseName("IX_AppUsers_EmployeeId");

        builder.HasIndex(u => u.CustomerId)
            .IsUnique()
            .HasFilter("[CustomerId] IS NOT NULL")
            .HasDatabaseName("IX_AppUsers_CustomerId");

        builder.HasIndex(u => new { u.TenantId, u.IsActive, u.IsDeleted })
            .HasDatabaseName("IX_AppUsers_TenantId_IsActive_IsDeleted");
    }
}
