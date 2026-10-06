using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class AppUserTotpSecretConfiguration : IEntityTypeConfiguration<AppUserTotpSecret>
{
    public void Configure(EntityTypeBuilder<AppUserTotpSecret> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AppUserTotpSecrets");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AppUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(s => s.EncryptedSecret)
            .IsRequired()
            .HasMaxLength(2048);

        builder.HasOne(s => s.AppUser)
            .WithMany()
            .HasForeignKey(s => s.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(s => !s.IsDeleted && !s.AppUser.IsDeleted);

        builder.HasIndex(s => new { s.AppUserId, s.IsActive, s.ExpiresAt })
            .HasDatabaseName("IX_AppUserTotpSecrets_AppUserId_IsActive_ExpiresAt");
    }
}
