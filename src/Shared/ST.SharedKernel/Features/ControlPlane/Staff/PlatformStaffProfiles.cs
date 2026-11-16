namespace ST.SharedKernel.Features.ControlPlane.Staff;

public static class PlatformStaffProfiles
{
    public static IReadOnlyList<PlatformStaffProfileResponse> All { get; } =
    [
        new(
            "administrator",
            "Platform Administrator",
            "Administer platform staff and supported platform operations.",
            PlatformPermissions.All.Order(StringComparer.Ordinal).ToArray()
        ),
        new(
            "onboarding",
            "Onboarding Officer",
            "Register customers and manage their initial administrator invitations.",
            [
                PlatformPermissions.ViewTenants,
                PlatformPermissions.CreateTenants,
                PlatformPermissions.EditTenants,
                PlatformPermissions.ManageTenantAdministrators,
                PlatformPermissions.ViewStamps,
                PlatformPermissions.ViewModules
            ]
        ),
        new(
            "kyc-collector",
            "KYC Collector",
            "Collect evidence and manage client submission links.",
            [
                PlatformPermissions.ViewKyc,
                PlatformPermissions.UploadKycDocuments,
                PlatformPermissions.ManageKycSubmissions
            ]
        ),
        new(
            "kyc-reviewer",
            "KYC Reviewer",
            "Review cleared evidence and explicitly approve customer KYC.",
            [
                PlatformPermissions.ViewKyc,
                PlatformPermissions.DownloadKycDocuments,
                PlatformPermissions.ReviewKycDocuments,
                PlatformPermissions.ApproveKyc
            ]
        ),
        new(
            "support",
            "Support Operator",
            "Request and use customer-approved, scoped support access.",
            [
                PlatformPermissions.ViewTenants,
                PlatformPermissions.ViewSupport,
                PlatformPermissions.ManageSupport
            ]
        ),
        new(
            "auditor",
            "Read-Only Auditor",
            "Read supported platform records without changing them or downloading originals.",
            [
                PlatformPermissions.ViewTenants,
                PlatformPermissions.ViewStamps,
                PlatformPermissions.ViewModules,
                PlatformPermissions.ViewAudit,
                PlatformPermissions.ViewKyc,
                PlatformPermissions.ViewSupport,
                PlatformPermissions.ViewPlatformStaff
            ]
        ),
        new(
            "retention",
            "Retention Operator",
            "Manage KYC retention policy and reasoned legal holds.",
            [
                PlatformPermissions.ViewKyc,
                PlatformPermissions.ManageKycRetention,
                PlatformPermissions.ManageKycHolds,
                PlatformPermissions.ViewAudit
            ]
        ),
        new(
            "lifecycle",
            "Lifecycle Operator",
            "Suspend/reactivate customers and manage their modules.",
            [
                PlatformPermissions.ViewTenants,
                PlatformPermissions.ManageTenantLifecycle,
                PlatformPermissions.ManageTenantModules,
                PlatformPermissions.ViewModules
            ]
        ),
        new(
            "tasks",
            "Platform Task Manager",
            "Manage platform task assignments and their supported queues.",
            [
                PlatformPermissions.ManageTasks,
                PlatformPermissions.ViewKyc,
                PlatformPermissions.ApproveKyc,
                PlatformPermissions.ViewTenants,
                PlatformPermissions.ManageTenantAdministrators,
                PlatformPermissions.ViewSupport,
                PlatformPermissions.ManageSupport
            ]
        ),
    ];
}
