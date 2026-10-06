using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class AppUserDeviceConfiguration : IEntityTypeConfiguration<AppUserDevice>
{
    public void Configure(EntityTypeBuilder<AppUserDevice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AppUserDevices");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.AppUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.DeviceFingerprint)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.DeviceName)
            .HasMaxLength(200);

        builder.Property(d => d.IpAddress)
            .HasMaxLength(64);

        builder.Property(d => d.UserAgent)
            .HasMaxLength(512);

        builder.HasOne(d => d.AppUser)
            .WithMany(u => u.TrustedDevices)
            .HasForeignKey(d => d.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(d => !d.IsDeleted && !d.AppUser.IsDeleted);

        builder.HasIndex(d => new { d.AppUserId, d.DeviceFingerprint })
            .IsUnique()
            .HasDatabaseName("IX_AppUserDevices_AppUserId_DeviceFingerprint");

        builder.HasIndex(d => new { d.AppUserId, d.IsTrusted, d.TrustedUntil })
            .HasDatabaseName("IX_AppUserDevices_AppUserId_IsTrusted_TrustedUntil");
    }
}
