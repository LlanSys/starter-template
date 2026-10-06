using ST.Domain.Features.Shared.EmailTemplates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.Shared.EmailTemplates.EntityConfigurations;

internal sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EmailTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Subject)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Body)
            .IsRequired();

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedBy)
            .HasMaxLength(100);

        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_EmailTemplates_Name");
    }
}
