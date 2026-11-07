namespace ST.SharedKernel.Features.ControlPlane.Support;
public sealed record SupportGrantResponse(Guid Id, Guid TenantId, string VendorId, string Reason, string[] PermissionKeys, bool ReadOnly, int DurationMinutes, string Status, string? ApprovedBy, DateTimeOffset? ExpiresAt);
