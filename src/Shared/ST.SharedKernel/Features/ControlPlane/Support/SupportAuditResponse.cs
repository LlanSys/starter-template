namespace ST.SharedKernel.Features.ControlPlane.Support;
public sealed record SupportAuditResponse(DateTimeOffset At, string ActorId, string Action, int? StatusCode);
