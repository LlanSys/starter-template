using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Shared.EmailTemplates.Entities;

public class EmailTemplate : BaseEntity, ISoftDeletable
{
    public string Name { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private EmailTemplate() { }

    public static EmailTemplate Create(string name, string subject, string body, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new EmailTemplate
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Subject = subject.Trim(),
            Body = body,
            CreatedBy = createdBy
        };
    }

    public void UpdateContent(string subject, string body, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Subject = subject.Trim();
        Body = body;
        SetUpdatedInfo(updatedBy);
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
