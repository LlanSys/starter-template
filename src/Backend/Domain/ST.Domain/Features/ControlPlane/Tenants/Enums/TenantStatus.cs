namespace ST.Domain.Features.ControlPlane.Tenants.Enums;

public enum TenantStatus
{
    PendingApproval = 0,
    PendingProvisioning = 1,
    Provisioning = 2,
    Active = 3,
    Suspended = 4,
    ProvisioningFailed = 5
}
