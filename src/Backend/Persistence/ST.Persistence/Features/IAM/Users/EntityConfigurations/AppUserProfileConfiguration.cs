using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class AppUserProfileConfiguration : IEntityTypeConfiguration<AppUserProfile>
{
    public void Configure(EntityTypeBuilder<AppUserProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AppUserProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.AppUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(p => p.TelephoneNo)
            .HasMaxLength(30);

        builder.Property(p => p.MobileNo)
            .HasMaxLength(30);

        builder.Property(p => p.Email)
            .HasMaxLength(200);

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedBy)
            .HasMaxLength(100);

        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<AppUserProfile>(p => p.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.AppUserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_AppUserProfiles_AppUserId");
    }
}
