using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.UI.Rcl.Features.IAM.Users.Contracts.Interfaces;

public interface IIamAdminService
{
    Task<AppResponse<PagedResponse<AdminUserListResponse, string>>> SearchUsersAsync(AdminUserSearchRequest request);

    Task<AppResponse<AppUserResponse>> CreateUserAsync(CreateAppUserRequest request);

    Task<AppResponse<AdminUserListResponse>> UpdateUserAsync(string userId, UpdateAdminUserRequest request);

    Task<AppResponse<bool>> DeactivateUserAsync(string userId, DeactivateUserRequest request);

    Task<AppResponse<UserRolesResponse>> GetUserRolesAsync(string userId);

    Task<AppResponse<UserRolesResponse>> UpdateUserRolesAsync(string userId, UpdateUserRolesRequest request);

    Task<AppResponse<UserPermissionsResponse>> GetUserPermissionsAsync(string userId);

    Task<AppResponse<UserPermissionsResponse>> UpdateUserPermissionsAsync(string userId, UpdateUserPermissionsRequest request);

    Task<AppResponse<bool>> GrantEmployeeSystemAccessAsync(Guid employeeId, GrantEmployeeSystemAccessRequest request);

    Task<AppResponse<bool>> RevokeEmployeeSystemAccessAsync(Guid employeeId, DeactivateUserRequest request);

    Task<AppResponse<IReadOnlyList<AdminRoleListResponse>>> GetRolesAsync();

    Task<AppResponse<AdminRoleListResponse>> CreateRoleAsync(CreateRoleRequest request);

    Task<AppResponse<AdminRoleListResponse>> UpdateRoleAsync(string roleId, UpdateRoleRequest request);

    Task<AppResponse<bool>> DeleteRoleAsync(string roleId);

    Task<AppResponse<RolePermissionsResponse>> GetRolePermissionsAsync(string roleId);

    Task<AppResponse<RolePermissionsResponse>> UpdateRolePermissionsAsync(string roleId, UpdateRolePermissionsRequest request);

    Task<AppResponse<PagedResponse<PermissionResponse, Guid>>> SearchPermissionsAsync(PermissionSearchRequest request);

    Task<AppResponse<IamReferenceDataResponse>> GetReferenceDataAsync();

    Task<AppResponse<IReadOnlyList<ReferenceCatalogItemResponse>>> GetReferenceCatalogAsync(string catalogType);

    Task<AppResponse<ReferenceCatalogItemResponse>> CreateReferenceCatalogItemAsync(string catalogType, ReferenceCatalogItemRequest request);

    Task<AppResponse<ReferenceCatalogItemResponse>> UpdateReferenceCatalogItemAsync(string catalogType, Guid id, ReferenceCatalogItemRequest request);

    Task<AppResponse<bool>> DeleteReferenceCatalogItemAsync(string catalogType, Guid id);

    Task<AppResponse<PermissionResponse>> GetPermissionByIdAsync(Guid permissionId);

    Task<AppResponse<PermissionResponse>> CreatePermissionAsync(CreatePermissionRequest request);

    Task<AppResponse<PermissionResponse>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request);

    Task<AppResponse<bool>> DeletePermissionAsync(Guid permissionId);

    Task<AppResponse<PagedResponse<MenuResponse, Guid>>> SearchMenusAsync(MenuSearchRequest request);

    Task<AppResponse<IReadOnlyList<MenuResponse>>> GetNavigationMenusAsync(string placement);

    Task<AppResponse<MenuResponse>> GetMenuByIdAsync(Guid menuId);

    Task<AppResponse<MenuResponse>> CreateMenuAsync(CreateMenuRequest request);

    Task<AppResponse<MenuResponse>> UpdateMenuAsync(Guid menuId, UpdateMenuRequest request);

    Task<AppResponse<bool>> DeleteMenuAsync(Guid menuId);

    Task<AppResponse<IReadOnlyList<AdminUserDeviceResponse>>> GetUserDevicesAsync();

    Task<AppResponse<bool>> RevokeUserDeviceAsync(Guid deviceId, RevokeUserDeviceRequest request);
}
