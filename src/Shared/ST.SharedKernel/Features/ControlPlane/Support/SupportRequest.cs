namespace ST.SharedKernel.Features.ControlPlane.Support;
public sealed record SupportRequest(Guid TenantId, string Reason, string[] PermissionKeys, bool ReadOnly = true, int DurationMinutes = 30);
