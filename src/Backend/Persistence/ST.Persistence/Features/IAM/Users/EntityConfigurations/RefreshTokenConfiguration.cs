using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.AppUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(64);

        builder.Property(t => t.RevokedByIp)
            .HasMaxLength(64);

        builder.Property(t => t.RevokedReason)
            .HasMaxLength(500);

        builder.Property(t => t.ReplacedByToken)
            .HasMaxLength(512);

        builder.Property(t => t.TokenFamily)
            .HasMaxLength(64);

        builder.HasOne(t => t.AppUser)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(t => !t.IsDeleted && !t.AppUser.IsDeleted);

        builder.HasIndex(t => t.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(t => new { t.AppUserId, t.ExpiresAt, t.RevokedAt })
            .HasDatabaseName("IX_RefreshTokens_AppUserId_ExpiresAt_RevokedAt");

        builder.HasIndex(t => new { t.TokenFamily, t.AppUserId })
            .HasDatabaseName("IX_RefreshTokens_TokenFamily_AppUserId");
    }
}
