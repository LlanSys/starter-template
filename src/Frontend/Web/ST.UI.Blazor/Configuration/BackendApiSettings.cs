using System.ComponentModel.DataAnnotations;

namespace ST.UI.Blazor.Configuration;

internal sealed class BackendApiSettings
{
    public const string SectionName = "BackendApi";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string TenantHeaderName { get; init; } = "X-Tenant-Id";

    [Required]
    public string Version { get; init; } = "1";

    [Range(0, 5)]
    public int TransientRetryCount { get; init; } = 2;

    [Range(50, 5_000)]
    public int TransientRetryDelayMilliseconds { get; init; } = 250;

    [Required]
    public BackendApiEndpoints Endpoints { get; init; } = new();
}

internal sealed class BackendApiEndpoints
{
    [Required]
    public IamEndpointSettings Iam { get; init; } = new();



    [Required]
    public SharedEndpointSettings Shared { get; init; } = new();
}

internal sealed class IamEndpointSettings
{
    [Required]
    public IamAuthEndpointSettings Auth { get; init; } = new();

    [Required]
    public IamUserEndpointSettings Users { get; init; } = new();

    [Required]
    public IamAdminEndpointSettings Admin { get; init; } = new();
}

internal sealed class IamAuthEndpointSettings
{
    public string Login { get; init; } = "api/v{version}/iam/auth/login";

    public string RefreshToken { get; init; } = "api/v{version}/iam/auth/refresh-token";

    public string AcceptInvitation { get; init; } = "api/v{version}/iam/auth/invitation/accept";

    public string ForgotPassword { get; init; } = "api/v{version}/iam/auth/password/forgot";

    public string ChangePassword { get; init; } = "api/v{version}/iam/users/password/change";

    public string VerifyPasswordResetOtp { get; init; } = "api/v{version}/iam/auth/password/reset/verify-otp";

    public string ResetPassword { get; init; } = "api/v{version}/iam/auth/password/reset";

    public string CurrentUser { get; init; } = "api/v{version}/iam/auth/me";

    public string UpdateProfilePicture { get; init; } = "api/v{version}/iam/users/me/profile-picture";

    public string ProfilePictureContent { get; init; } = "api/v{version}/iam/users/me/profile-picture/content";

    public string Logout { get; init; } = "api/v{version}/iam/auth/logout";

    public string PasskeyRegistrationOptions { get; init; } = "api/v{version}/iam/auth/passkey/register/options";

    public string PasskeyRegister { get; init; } = "api/v{version}/iam/auth/passkey/register";

    public string PasskeyLoginOptions { get; init; } = "api/v{version}/iam/auth/passkey/login/options";

    public string PasskeyLogin { get; init; } = "api/v{version}/iam/auth/passkey/login";

    public string Passkeys { get; init; } = "api/v{version}/iam/auth/passkeys";
}

internal sealed class IamUserEndpointSettings
{
    public string InitiateTotpSetup { get; init; } = "api/v{version}/iam/users/totp/setup";

    public string VerifyTotp { get; init; } = "api/v{version}/iam/users/totp/verify";

    public string DisableTotp { get; init; } = "api/v{version}/iam/users/totp/disable";

    public string TotpStatus { get; init; } = "api/v{version}/iam/users/totp/{userId}/status";
}

internal sealed class IamAdminEndpointSettings
{
    public string Users { get; init; } = "api/v{version}/iam/admin/users";

    public string UserUpdate { get; init; } = "api/v{version}/iam/admin/users/{userId}";

    public string UserInvite { get; init; } = "api/v{version}/iam/admin/users/{userId}/invite";

    public string UserActivate { get; init; } = "api/v{version}/iam/admin/users/{userId}/activate";

    public string UserDeactivate { get; init; } = "api/v{version}/iam/admin/users/{userId}/deactivate";

    public string UserRoles { get; init; } = "api/v{version}/iam/admin/users/{userId}/roles";

    public string UserRolesUpdate { get; init; } = "api/v{version}/iam/admin/users/{userId}/roles";

    public string UserPermissions { get; init; } = "api/v{version}/iam/admin/users/{userId}/permissions";

    public string UserPermissionsUpdate { get; init; } = "api/v{version}/iam/admin/users/{userId}/permissions";



    public string Roles { get; init; } = "api/v{version}/iam/admin/roles";

    public string RoleUpdate { get; init; } = "api/v{version}/iam/admin/roles/{roleId}";

    public string RoleDelete { get; init; } = "api/v{version}/iam/admin/roles/{roleId}";

    public string RolePermissions { get; init; } = "api/v{version}/iam/admin/roles/{roleId}/permissions";

    public string RolePermissionsUpdate { get; init; } = "api/v{version}/iam/admin/roles/{roleId}/permissions";

    public string Permissions { get; init; } = "api/v{version}/iam/admin/permissions";

    public string ReferenceData { get; init; } = "api/v{version}/iam/admin/reference-data";

    public string ReferenceCatalog { get; init; } = "api/v{version}/iam/admin/reference-catalogs/{catalogType}";

    public string ReferenceCatalogItem { get; init; } = "api/v{version}/iam/admin/reference-catalogs/{catalogType}/{id}";

    public string PermissionDetail { get; init; } = "api/v{version}/iam/admin/permissions/{permissionId}";

    public string PermissionCreate { get; init; } = "api/v{version}/iam/admin/permissions";

    public string PermissionUpdate { get; init; } = "api/v{version}/iam/admin/permissions/{permissionId}";

    public string PermissionDelete { get; init; } = "api/v{version}/iam/admin/permissions/{permissionId}";

    public string Menus { get; init; } = "api/v{version}/iam/admin/menus";

    public string MenuNavigation { get; init; } = "api/v{version}/iam/admin/menus/navigation/{placement}";

    public string MenuDetail { get; init; } = "api/v{version}/iam/admin/menus/{menuId}";

    public string MenuCreate { get; init; } = "api/v{version}/iam/admin/menus";

    public string MenuUpdate { get; init; } = "api/v{version}/iam/admin/menus/{menuId}";

    public string MenuDelete { get; init; } = "api/v{version}/iam/admin/menus/{menuId}";

    public string UserDevices { get; init; } = "api/v{version}/iam/admin/user-devices";

    public string UserDeviceRevoke { get; init; } = "api/v{version}/iam/admin/user-devices/{deviceId}/revoke";
}



internal sealed class SharedEndpointSettings
{
    [Required]
    public PaymentEndpointSettings Payments { get; init; } = new();

    [Required]
    public OrgSettingsEndpointSettings OrgSettings { get; init; } = new();

    public string LookupCatalogTypes { get; init; } = "api/v{version}/shared/lookups/catalog-types";

    public string LookupByType { get; init; } = "api/v{version}/shared/lookups/{lookupType}";

    public string LookupCreate { get; init; } = "api/v{version}/shared/lookups/{lookupType}";

    public string LookupUpdate { get; init; } = "api/v{version}/shared/lookups/{lookupType}/{id}";

    public string LookupDelete { get; init; } = "api/v{version}/shared/lookups/{lookupType}/{id}";
}

internal sealed class OrgSettingsEndpointSettings
{
    public string Root { get; init; } = "api/v{version}/shared/tenant-settings";
    public string Detail { get; init; } = "api/v{version}/shared/tenant-settings/{key}";
}

internal sealed class PaymentEndpointSettings
{
    public string Checkout { get; init; } = "api/v{version}/shared/payments/checkout";

    public string Status { get; init; } = "api/v{version}/shared/payments/{provider}/{paymentReference}";

    public string Capabilities { get; init; } = "api/v{version}/shared/payments/capabilities";

    public string History { get; init; } = "api/v{version}/shared/payments/history";

    public string RegisterMpesaC2BUrls { get; init; } = "api/v{version}/shared/payments/mpesa/c2b/register-urls";

    public string SimulateMpesaC2B { get; init; } = "api/v{version}/shared/payments/mpesa/c2b/simulate";
}
