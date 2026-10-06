using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.UI.Blazor.Configuration;
using ST.UI.Blazor.Features.Shared.BackendApi;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Rcl.Features.IAM.Users.Contracts.Interfaces;
using Microsoft.Extensions.Options;

namespace ST.UI.Blazor.Features.IAM.Users.Contracts.Implementations;

internal sealed class IamAdminService(IBackendApiClient apiClient, IOptions<BackendApiSettings> apiSettings) : IIamAdminService
{
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;

    public Task<AppResponse<PagedResponse<AdminUserListResponse, string>>> SearchUsersAsync(AdminUserSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<PagedResponse<AdminUserListResponse, string>>(
            HttpMethod.Get,
            $"{EndpointFormatter.Format(_apiSettings.Endpoints.Iam.Admin.Users, _apiSettings.Version)}{request.BuildQueryString()}",
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");
    }

    public Task<AppResponse<AppUserResponse>> CreateUserAsync(CreateAppUserRequest request)
        => apiClient.SendAsync<AppUserResponse>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Iam.Admin.Users, _apiSettings.Version),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<AdminUserListResponse>> UpdateUserAsync(string userId, UpdateAdminUserRequest request)
        => apiClient.SendAsync<AdminUserListResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.UserUpdate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["userId"] = userId }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<bool>> ActivateUserAsync(string userId)
        => apiClient.SendAsync<bool>(
            HttpMethod.Patch,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.UserActivate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["userId"] = userId }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");
    public Task<AppResponse<bool>> DeactivateUserAsync(string userId, DeactivateUserRequest request)
        => apiClient.SendAsync<bool>(
            HttpMethod.Patch,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.UserDeactivate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["userId"] = userId }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<UserRolesResponse>> GetUserRolesAsync(string userId)
        => apiClient.SendAsync<UserRolesResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.UserRoles,
                _apiSettings.Version,
                new Dictionary<string, string> { ["userId"] = userId }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<UserRolesResponse>> UpdateUserRolesAsync(string userId, UpdateUserRolesRequest request)
        => apiClient.SendAsync<UserRolesResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.UserRolesUpdate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["userId"] = userId }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<UserPermissionsResponse>> GetUserPermissionsAsync(string userId)
        => apiClient.SendAsync<UserPermissionsResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.UserPermissions,
                _apiSettings.Version,
                new Dictionary<string, string> { ["userId"] = userId }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<UserPermissionsResponse>> UpdateUserPermissionsAsync(string userId, UpdateUserPermissionsRequest request)
        => apiClient.SendAsync<UserPermissionsResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.UserPermissionsUpdate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["userId"] = userId }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<bool>> GrantEmployeeSystemAccessAsync(Guid employeeId, GrantEmployeeSystemAccessRequest request)
        => apiClient.SendAsync<bool>(
            HttpMethod.Post,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.EmployeeGrantAccess,
                _apiSettings.Version,
                new Dictionary<string, string> { ["employeeId"] = employeeId.ToString() }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<bool>> RevokeEmployeeSystemAccessAsync(Guid employeeId, DeactivateUserRequest request)
        => apiClient.SendAsync<bool>(
            HttpMethod.Patch,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.EmployeeRevokeAccess,
                _apiSettings.Version,
                new Dictionary<string, string> { ["employeeId"] = employeeId.ToString() }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<IReadOnlyList<AdminRoleListResponse>>> GetRolesAsync()
        => GetAsync<AdminRoleListResponse>(_apiSettings.Endpoints.Iam.Admin.Roles);

    public Task<AppResponse<AdminRoleListResponse>> CreateRoleAsync(CreateRoleRequest request)
        => apiClient.SendAsync<AdminRoleListResponse>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Iam.Admin.Roles, _apiSettings.Version),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<AdminRoleListResponse>> UpdateRoleAsync(string roleId, UpdateRoleRequest request)
        => apiClient.SendAsync<AdminRoleListResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.RoleUpdate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["roleId"] = roleId }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<bool>> DeleteRoleAsync(string roleId)
        => apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.RoleDelete,
                _apiSettings.Version,
                new Dictionary<string, string> { ["roleId"] = roleId }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<RolePermissionsResponse>> GetRolePermissionsAsync(string roleId)
        => apiClient.SendAsync<RolePermissionsResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.RolePermissions,
                _apiSettings.Version,
                new Dictionary<string, string> { ["roleId"] = roleId }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<RolePermissionsResponse>> UpdateRolePermissionsAsync(string roleId, UpdateRolePermissionsRequest request)
        => apiClient.SendAsync<RolePermissionsResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.RolePermissionsUpdate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["roleId"] = roleId }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<PagedResponse<PermissionResponse, Guid>>> SearchPermissionsAsync(PermissionSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<PagedResponse<PermissionResponse, Guid>>(
            HttpMethod.Get,
            $"{EndpointFormatter.Format(_apiSettings.Endpoints.Iam.Admin.Permissions, _apiSettings.Version)}{request.BuildQueryString()}",
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");
    }

    public Task<AppResponse<IamReferenceDataResponse>> GetReferenceDataAsync()
        => apiClient.SendAsync<IamReferenceDataResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(_apiSettings.Endpoints.Iam.Admin.ReferenceData, _apiSettings.Version),
            unavailableMessage: "The IAM reference-data service is unavailable. Please try again.",
            timeoutMessage: "The IAM reference-data service timed out. Please try again.");

    public Task<AppResponse<IReadOnlyList<ReferenceCatalogItemResponse>>> GetReferenceCatalogAsync(string catalogType)
        => apiClient.SendAsync<IReadOnlyList<ReferenceCatalogItemResponse>>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.ReferenceCatalog,
                _apiSettings.Version,
                new Dictionary<string, string> { ["catalogType"] = catalogType }),
            unavailableMessage: "The IAM reference-catalog service is unavailable. Please try again.",
            timeoutMessage: "The IAM reference-catalog service timed out. Please try again.");

    public Task<AppResponse<ReferenceCatalogItemResponse>> CreateReferenceCatalogItemAsync(string catalogType, ReferenceCatalogItemRequest request)
        => apiClient.SendAsync<ReferenceCatalogItemResponse>(
            HttpMethod.Post,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.ReferenceCatalog,
                _apiSettings.Version,
                new Dictionary<string, string> { ["catalogType"] = catalogType }),
            request,
            unavailableMessage: "The IAM reference-catalog service is unavailable. Please try again.",
            timeoutMessage: "The IAM reference-catalog service timed out. Please try again.");

    public Task<AppResponse<ReferenceCatalogItemResponse>> UpdateReferenceCatalogItemAsync(string catalogType, Guid id, ReferenceCatalogItemRequest request)
        => apiClient.SendAsync<ReferenceCatalogItemResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.ReferenceCatalogItem,
                _apiSettings.Version,
                new Dictionary<string, string> { ["catalogType"] = catalogType, ["id"] = id.ToString() }),
            request,
            unavailableMessage: "The IAM reference-catalog service is unavailable. Please try again.",
            timeoutMessage: "The IAM reference-catalog service timed out. Please try again.");

    public Task<AppResponse<bool>> DeleteReferenceCatalogItemAsync(string catalogType, Guid id)
        => apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.ReferenceCatalogItem,
                _apiSettings.Version,
                new Dictionary<string, string> { ["catalogType"] = catalogType, ["id"] = id.ToString() }),
            unavailableMessage: "The IAM reference-catalog service is unavailable. Please try again.",
            timeoutMessage: "The IAM reference-catalog service timed out. Please try again.");

    public Task<AppResponse<PermissionResponse>> GetPermissionByIdAsync(Guid permissionId)
        => apiClient.SendAsync<PermissionResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.PermissionDetail,
                _apiSettings.Version,
                new Dictionary<string, string> { ["permissionId"] = permissionId.ToString() }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<PermissionResponse>> CreatePermissionAsync(CreatePermissionRequest request)
        => apiClient.SendAsync<PermissionResponse>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Iam.Admin.PermissionCreate, _apiSettings.Version),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<PermissionResponse>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request)
        => apiClient.SendAsync<PermissionResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.PermissionUpdate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["permissionId"] = permissionId.ToString() }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<bool>> DeletePermissionAsync(Guid permissionId)
        => apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.PermissionDelete,
                _apiSettings.Version,
                new Dictionary<string, string> { ["permissionId"] = permissionId.ToString() }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<PagedResponse<MenuResponse, Guid>>> SearchMenusAsync(MenuSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<PagedResponse<MenuResponse, Guid>>(
            HttpMethod.Get,
            $"{EndpointFormatter.Format(_apiSettings.Endpoints.Iam.Admin.Menus, _apiSettings.Version)}{request.BuildQueryString()}",
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");
    }

    public Task<AppResponse<IReadOnlyList<MenuResponse>>> GetNavigationMenusAsync(string placement)
        => apiClient.SendAsync<IReadOnlyList<MenuResponse>>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.MenuNavigation,
                _apiSettings.Version,
                new Dictionary<string, string> { ["placement"] = placement }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<MenuResponse>> GetMenuByIdAsync(Guid menuId)
        => apiClient.SendAsync<MenuResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.MenuDetail,
                _apiSettings.Version,
                new Dictionary<string, string> { ["menuId"] = menuId.ToString() }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<MenuResponse>> CreateMenuAsync(CreateMenuRequest request)
        => apiClient.SendAsync<MenuResponse>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Iam.Admin.MenuCreate, _apiSettings.Version),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<MenuResponse>> UpdateMenuAsync(Guid menuId, UpdateMenuRequest request)
        => apiClient.SendAsync<MenuResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.MenuUpdate,
                _apiSettings.Version,
                new Dictionary<string, string> { ["menuId"] = menuId.ToString() }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<bool>> DeleteMenuAsync(Guid menuId)
        => apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.MenuDelete,
                _apiSettings.Version,
                new Dictionary<string, string> { ["menuId"] = menuId.ToString() }),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    public Task<AppResponse<IReadOnlyList<AdminUserDeviceResponse>>> GetUserDevicesAsync()
        => GetAsync<AdminUserDeviceResponse>(_apiSettings.Endpoints.Iam.Admin.UserDevices);

    public Task<AppResponse<bool>> RevokeUserDeviceAsync(Guid deviceId, RevokeUserDeviceRequest request)
        => apiClient.SendAsync<bool>(
            HttpMethod.Patch,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Iam.Admin.UserDeviceRevoke,
                _apiSettings.Version,
                new Dictionary<string, string> { ["deviceId"] = deviceId.ToString() }),
            request,
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");

    private Task<AppResponse<IReadOnlyList<T>>> GetAsync<T>(string endpoint)
        => apiClient.SendAsync<IReadOnlyList<T>>(
            HttpMethod.Get,
            EndpointFormatter.Format(endpoint, _apiSettings.Version),
            unavailableMessage: "The IAM administration service is unavailable. Please try again.",
            timeoutMessage: "The IAM administration service timed out. Please try again.");
}
