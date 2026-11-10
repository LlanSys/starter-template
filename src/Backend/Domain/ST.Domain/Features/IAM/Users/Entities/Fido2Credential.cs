using ST.Domain.Shared.Entities;
using System;
using System.Text.Json.Serialization;

namespace ST.Domain.Features.IAM.Users.Entities;

using ST.Domain.Shared.Contracts.Common;

public class Fido2Credential : BaseEntity, ISoftDeletable
{
    public string UserId { get; set; } = string.Empty;
    public byte[] PublicKey { get; set; } = [];
    public byte[] UserHandle { get; set; } = [];
    public byte[] CredentialId { get; set; } = [];
    public uint SignatureCounter { get; set; }
    public string CredType { get; set; } = string.Empty;
    public DateTimeOffset RegDate { get; set; }
    public Guid AaGuid { get; set; }

    [JsonIgnore]
    public virtual AppUser? User { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
    }
}
