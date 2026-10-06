using ST.Domain.Features.Shared.FailedMessages.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.Shared.FailedMessages.EntityConfigurations;

internal sealed class FailedMessageConfiguration : IEntityTypeConfiguration<FailedMessage>
{
    public void Configure(EntityTypeBuilder<FailedMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FailedMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MessageId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(m => m.MessageType)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(m => m.EntityId)
            .HasMaxLength(100);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.ErrorMessage)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(m => m.ResolutionNotes)
            .HasMaxLength(1000);

        builder.HasIndex(m => m.MessageId)
            .HasDatabaseName("IX_FailedMessages_MessageId");

        builder.HasIndex(m => new { m.Status, m.IsResolved, m.FailedAt })
            .HasDatabaseName("IX_FailedMessages_Status_IsResolved_FailedAt");
    }
}
