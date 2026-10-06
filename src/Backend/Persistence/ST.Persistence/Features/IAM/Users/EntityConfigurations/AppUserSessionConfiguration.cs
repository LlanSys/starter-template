using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class AppUserSessionConfiguration : IEntityTypeConfiguration<AppUserSession>
{
    public void Configure(EntityTypeBuilder<AppUserSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AppUserSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AppUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(64);

        builder.Property(s => s.UserAgent)
            .HasMaxLength(512);

        builder.Property(s => s.EndReason)
            .HasMaxLength(500);

        builder.Property(s => s.DeviceFingerprint)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne(s => s.AppUser)
            .WithMany()
            .HasForeignKey(s => s.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(s => !s.IsDeleted && !s.AppUser.IsDeleted);

        builder.HasIndex(s => new { s.AppUserId, s.IsActive, s.ExpiresAt })
            .HasDatabaseName("IX_AppUserSessions_AppUserId_IsActive_ExpiresAt");

        builder.HasIndex(s => new { s.AppUserId, s.DeviceFingerprint, s.IsActive })
            .HasDatabaseName("IX_AppUserSessions_AppUserId_DeviceFingerprint_IsActive");

        builder.HasIndex(s => new { s.IsActive, s.IsRevoked, s.ExpiresAt })
            .HasDatabaseName("IX_AppUserSessions_IsActive_IsRevoked_ExpiresAt");
    }
}
