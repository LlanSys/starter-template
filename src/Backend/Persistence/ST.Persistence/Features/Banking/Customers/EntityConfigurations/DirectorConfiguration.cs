using ST.Domain.Features.Banking.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.Banking.Customers.EntityConfigurations;

internal sealed class DirectorConfiguration : IEntityTypeConfiguration<Director>
{
    public void Configure(EntityTypeBuilder<Director> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Directors");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.RelationType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.IdentificationType)
            .HasConversion<string>()
            .HasMaxLength(80);

        builder.Property(d => d.IdentificationNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(d => d.Email)
            .HasMaxLength(200);

        builder.Property(d => d.SharePercentage)
            .HasPrecision(5, 2);

        builder.Property(d => d.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(d => d.DeletedBy)
            .HasMaxLength(100);

        builder.HasIndex(d => d.CustomerId)
            .HasDatabaseName("IX_Directors_CustomerId");

        builder.HasIndex(d => new { d.CustomerId, d.IdentificationNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Directors_CustomerId_IdentificationNumber");
    }
}
