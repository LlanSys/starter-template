using ST.Domain.Features.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.IAM.Users.EntityConfigurations;

internal sealed class TempTotpSecretConfiguration : IEntityTypeConfiguration<TempTotpSecret>
{
    public void Configure(EntityTypeBuilder<TempTotpSecret> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TempTotpSecrets");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(s => s.EncryptedSecret)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(s => s.DeletedBy)
            .HasMaxLength(100);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.UserId, s.IsDeleted, s.ExpiresAt })
            .HasDatabaseName("IX_TempTotpSecrets_UserId_IsDeleted_ExpiresAt");
    }
}
