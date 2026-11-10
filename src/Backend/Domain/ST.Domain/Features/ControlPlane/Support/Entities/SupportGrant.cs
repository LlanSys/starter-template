using ST.Domain.Shared.Contracts.Common;
namespace ST.Domain.Features.ControlPlane.Support.Entities;
public sealed class SupportGrant : IAuditable
{
 public Guid Id { get; set; } = Guid.CreateVersion7();
 public Guid WorkspaceId { get; set; }
 public string VendorId { get; set; } = "";
 public string Reason { get; set; } = "";
 public string[] PermissionKeys { get; set; } = [];
 public bool ReadOnly { get; set; } = true;
 public int DurationMinutes { get; set; } = 30;
 public string Status { get; set; } = "Pending";
 public string? ApprovedBy { get; set; }
 public DateTimeOffset? ExpiresAt { get; set; }
 public string? ProxyUserId { get; set; }
 public DateTimeOffset CreatedAt { get; set; }
 public string CreatedBy { get; set; } = "";
 public DateTimeOffset? UpdatedAt { get; set; }
 public string? UpdatedBy { get; set; }
 public byte[] RowVersion { get; set; } = [];
 public bool IsLive(DateTimeOffset now) => Status == "Approved" && ExpiresAt > now;
}
