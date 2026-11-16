using System.Collections.Frozen;
using ST.SharedKernel.Features.ControlPlane;

namespace ST.SharedKernel.Features.Shared.Dashboard;

public static class DashboardTaskCatalog
{
    public const string CustomerAssignmentPermission = "tasks.assign";
    public static IReadOnlyDictionary<string, DashboardTaskDefinition> Definitions { get; } = new[]
    {
        new DashboardTaskDefinition("kyc-review", "KYC Approval Outstanding", "KYC approval has not been recorded. Evidence may still be incomplete.", "Review KYC case", true, [PlatformPermissions.ViewKyc, PlatformPermissions.ApproveKyc]),
        new DashboardTaskDefinition("provisioning-failed", "Provisioning Failure", "Workspace provisioning did not complete successfully.", "Review failure before retrying", true, [PlatformPermissions.ViewTenants, PlatformPermissions.ManageTenantLifecycle]),
        new DashboardTaskDefinition("invitation-delivery", "Administrator Invitation", "An invitation delivery attempt was unsuccessful or an invitation has expired.", "Review administrator invitation", true, [PlatformPermissions.ViewTenants, PlatformPermissions.ManageTenantAdministrators]),
        new DashboardTaskDefinition("hold-review", "Legal Hold Review", "The scheduled review date for this legal hold has arrived.", "Review the hold and record a decision", true, [PlatformPermissions.ViewKyc, PlatformPermissions.ManageKycHolds]),
        new DashboardTaskDefinition("support-approval", "Support Access Request", "Support access has been requested for this workspace and requires approval.", "Review scope and duration", false, ["support.approve"])
    }.ToFrozenDictionary(item => item.Kind, StringComparer.Ordinal);
}
