namespace ST.Domain.Features.ControlPlane.Support.Entities;
public sealed class SupportAudit
{
 public Guid Id { get; set; } = Guid.CreateVersion7();
 public Guid GrantId { get; set; }
 public Guid WorkspaceId { get; set; }
 public string ActorId { get; set; } = "";
 public string Action { get; set; } = "";
 public int? StatusCode { get; set; }
 public DateTimeOffset CreatedAt { get; set; }
}
