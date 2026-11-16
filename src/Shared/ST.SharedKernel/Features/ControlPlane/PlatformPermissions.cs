using System.Collections.Frozen;

namespace ST.SharedKernel.Features.ControlPlane;

public static class PlatformPermissions
{
    public const string ViewPlatformStaff = "control_plane.view_platform_staff";
    public const string ManagePlatformStaff = "control_plane.manage_platform_staff";
    public const string LegacyManage = "control_plane.manage";
    public const string ManageTasks = "control_plane.manage_tasks";
    public const string ViewTenants = "control_plane.view_tenants";
    public const string CreateTenants = "control_plane.create_tenants";
    public const string EditTenants = "control_plane.edit_tenants";
    public const string ManageTenantAdministrators = "control_plane.manage_tenant_administrators";
    public const string ManageTenantLifecycle = "control_plane.manage_tenant_lifecycle";
    public const string ManageTenantModules = "control_plane.manage_tenant_modules";
    public const string MigrateTenants = "control_plane.migrate_tenants";
    public const string ViewStamps = "control_plane.view_stamps";
    public const string ManageStamps = "control_plane.manage_stamps";
    public const string ViewModules = "control_plane.view_modules";
    public const string ManageModules = "control_plane.manage_modules";
    public const string ViewAudit = "control_plane.view_audit";
    public const string ViewSupport = "control_plane.view_support";
    public const string ManageSupport = "control_plane.manage_support";
    public const string EnterDemo = "control_plane.enter_demo";
    public const string ResetDemo = "control_plane.reset_demo";
    public const string ViewKyc = "control_plane.view_kyc";
    public const string UploadKycDocuments = "control_plane.upload_kyc_documents";
    public const string DownloadKycDocuments = "control_plane.download_kyc_documents";
    public const string ReviewKycDocuments = "control_plane.review_kyc_documents";
    public const string ManageKycSubmissions = "control_plane.manage_kyc_submissions";
    public const string ApproveKyc = "control_plane.approve_kyc";
    public const string ManageKycRetention = "control_plane.manage_kyc_retention";
    public const string ManageKycHolds = "control_plane.manage_kyc_holds";

    public static IReadOnlySet<string> All
    {
        get;
    } = new[]
    {
        ViewPlatformStaff,
        ManagePlatformStaff,
        ManageTasks,
        ViewTenants,
        CreateTenants,
        EditTenants,
        ManageTenantAdministrators,
        ManageTenantLifecycle,
        ManageTenantModules,
        MigrateTenants,
        ViewStamps,
        ManageStamps,
        ViewModules,
        ManageModules,
        ViewAudit,
        ViewSupport,
        ManageSupport,
        EnterDemo,
        ResetDemo,
        ViewKyc,
        UploadKycDocuments,
        DownloadKycDocuments,
        ReviewKycDocuments,
        ManageKycSubmissions,
        ApproveKyc,
        ManageKycRetention,
        ManageKycHolds,
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsDefined(string permission)
    {
        return All.Contains(permission);
    }

    public static bool OpensControlPanel(IEnumerable<string> permissions)
    {
        return permissions.Any(All.Contains);
    }
}
