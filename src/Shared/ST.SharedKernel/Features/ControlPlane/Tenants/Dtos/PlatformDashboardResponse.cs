namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record PlatformDashboardResponse(
    int CustomerTenants,
    int ActiveTenants,
    int SuspendedTenants,
    int AwaitingKyc,
    int PendingProvisioning,
    int Provisioning,
    int ProvisioningFailed,
    int? InvitationsPending,
    int? InvitationDeliveryRetries);
