using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.IAM.Menus.Commands;
using ST.Application.Features.IAM.Menus.Queries;
using ST.Application.Features.IAM.Permissions.Commands;
using ST.Application.Features.IAM.Permissions.Queries;
using ST.Application.Features.IAM.ReferenceData.Commands;
using ST.Application.Features.IAM.ReferenceData.Queries;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Queries;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ST.Api.Features.IAM.Users.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iam/admin")]
[ApiController]
[Authorize]
public sealed class IamAdminController(ISender sender) : BaseController
{
    [HttpGet("users")]
    [RequirePermission("users.view")]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserSearchRequest request)
        => HandleResponse(await sender.Send(new GetAdminUsersQuery(request)).ConfigureAwait(false));

    [HttpPost("users")]
    [RequirePermission("users.create")]
    public async Task<IActionResult> CreateUser(CreateAppUserRequest request)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new CreateAppUserCommand(request, userId)).ConfigureAwait(false));
    }

    [HttpPut("users/{userId}")]
    [RequirePermission("users.edit")]
    public async Task<IActionResult> UpdateUser(string userId, UpdateAdminUserRequest request)
    {
        var updatedBy = GetActorId();
        return HandleResponse(await sender.Send(new UpdateAdminUserCommand(userId, request, updatedBy)).ConfigureAwait(false));
    }

    [HttpPost("users/{userId}/invite")]
    [RequirePermission("users.edit")]
    public async Task<IActionResult> InviteUser(string userId, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new InviteAdminUserCommand(userId), cancellationToken).ConfigureAwait(false));

    [HttpPatch("users/{userId}/activate")]
    [RequirePermission("users.edit")]
    public async Task<IActionResult> ActivateUser(string userId, CancellationToken cancellationToken)
    {
        var activatedBy = GetActorId();
        return HandleResponse(await sender.Send(new ActivateAdminUserCommand(userId, activatedBy), cancellationToken).ConfigureAwait(false));
    }
    [HttpPatch("users/{userId}/deactivate")]
    [RequirePermission("users.deactivate")]
    public async Task<IActionResult> DeactivateUser(string userId, DeactivateUserRequest request, CancellationToken cancellationToken)
    {
        var deactivatedBy = GetActorId();
        return HandleResponse(await sender.Send(new DeactivateAdminUserCommand(userId, request, deactivatedBy), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("users/{userId}/roles")]
    [RequirePermission("users.manage_roles")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        var requestedBy = GetActorId();
        return HandleResponse(await sender.Send(new GetUserRolesQuery(userId, requestedBy)).ConfigureAwait(false));
    }

    [HttpPut("users/{userId}/roles")]
    [RequirePermission("users.manage_roles")]
    public async Task<IActionResult> UpdateUserRoles(string userId, UpdateUserRolesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var updatedBy = GetActorId();
        return HandleResponse(await sender.Send(new UpdateUserRolesCommand(userId, request, updatedBy)).ConfigureAwait(false));
    }

    [HttpGet("users/{userId}/permissions")]
    [RequirePermission("users.manage_permissions")]
    public async Task<IActionResult> GetUserPermissions(string userId)
    {
        var requestedBy = GetActorId();
        return HandleResponse(await sender.Send(new GetUserPermissionsQuery(userId, requestedBy)).ConfigureAwait(false));
    }

    [HttpPut("users/{userId}/permissions")]
    [RequirePermission("users.manage_permissions")]
    public async Task<IActionResult> UpdateUserPermissions(string userId, UpdateUserPermissionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var updatedBy = GetActorId();
        return HandleResponse(await sender.Send(new UpdateUserPermissionsCommand(userId, request, updatedBy)).ConfigureAwait(false));
    }

    [HttpGet("roles")]
    [RequirePermission("roles.view")]
    public async Task<IActionResult> GetRoles()
        => HandleResponse(await sender.Send(new GetAdminRolesQuery()).ConfigureAwait(false));

    [HttpPost("roles")]
    [RequirePermission("roles.create")]
    public async Task<IActionResult> CreateRole(CreateRoleRequest request)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new CreateRoleCommand(request, userId)).ConfigureAwait(false));
    }

    [HttpPut("roles/{roleId}")]
    [RequirePermission("roles.edit")]
    public async Task<IActionResult> UpdateRole(string roleId, UpdateRoleRequest request)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new UpdateRoleCommand(roleId, request, userId)).ConfigureAwait(false));
    }

    [HttpDelete("roles/{roleId}")]
    [RequirePermission("roles.delete")]
    public async Task<IActionResult> DeleteRole(string roleId)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new DeleteRoleCommand(roleId, userId)).ConfigureAwait(false));
    }





    [HttpGet("roles/{roleId}/permissions")]
    [RequirePermission("roles.manage_permissions")]
    public async Task<IActionResult> GetRolePermissions(string roleId)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new GetRolePermissionsQuery(roleId, userId)).ConfigureAwait(false));
    }

    [HttpPut("roles/{roleId}/permissions")]
    [RequirePermission("roles.manage_permissions")]
    public async Task<IActionResult> UpdateRolePermissions(string roleId, UpdateRolePermissionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetActorId();
        return HandleResponse(await sender.Send(new UpdateRolePermissionsCommand(roleId, request, userId)).ConfigureAwait(false));
    }

    [HttpGet("permissions")]
    [RequirePermission("roles.manage_permissions")]
    public async Task<IActionResult> SearchPermissions([FromQuery] PermissionSearchRequest request)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new SearchPermissionsQuery(request, userId)).ConfigureAwait(false));
    }

    [HttpGet("reference-data")]
    [RequirePermission("menus.view")]
    public async Task<IActionResult> GetReferenceData()
        => HandleResponse(await sender.Send(new GetIamReferenceDataQuery()).ConfigureAwait(false));

    [HttpGet("reference-catalogs/{catalogType}")]
    [RequirePermission("menus.view")]
    public async Task<IActionResult> GetReferenceCatalog(string catalogType)
        => HandleResponse(await sender.Send(new GetReferenceCatalogQuery(catalogType)).ConfigureAwait(false));

    [HttpPost("reference-catalogs/{catalogType}")]
    [RequirePermission("menus.create")]
    public async Task<IActionResult> CreateReferenceCatalogItem(string catalogType, ReferenceCatalogItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetActorId();
        return HandleResponse(await sender.Send(new CreateReferenceCatalogItemCommand(catalogType, request, userId)).ConfigureAwait(false));
    }

    [HttpPut("reference-catalogs/{catalogType}/{id:guid}")]
    [RequirePermission("menus.edit")]
    public async Task<IActionResult> UpdateReferenceCatalogItem(string catalogType, Guid id, ReferenceCatalogItemRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetActorId();
        return HandleResponse(await sender.Send(new UpdateReferenceCatalogItemCommand(catalogType, id, request, userId)).ConfigureAwait(false));
    }

    [HttpDelete("reference-catalogs/{catalogType}/{id:guid}")]
    [RequirePermission("menus.delete")]
    public async Task<IActionResult> DeleteReferenceCatalogItem(string catalogType, Guid id)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new DeleteReferenceCatalogItemCommand(catalogType, id, userId)).ConfigureAwait(false));
    }

    [HttpGet("permissions/{permissionId:guid}")]
    [RequirePermission("roles.manage_permissions")]
    public async Task<IActionResult> GetPermissionById(Guid permissionId)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new GetPermissionByIdQuery(permissionId, userId)).ConfigureAwait(false));
    }

    [HttpPost("permissions")]
    [RequirePermission("roles.manage_permissions")]
    public async Task<IActionResult> CreatePermission(CreatePermissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetActorId();
        return HandleResponse(await sender.Send(new CreatePermissionCommand(request, userId)).ConfigureAwait(false));
    }

    [HttpPut("permissions/{permissionId:guid}")]
    [RequirePermission("roles.manage_permissions")]
    public async Task<IActionResult> UpdatePermission(Guid permissionId, UpdatePermissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetActorId();
        return HandleResponse(await sender.Send(new UpdatePermissionCommand(permissionId, request with { Id = permissionId }, userId)).ConfigureAwait(false));
    }

    [HttpDelete("permissions/{permissionId:guid}")]
    [RequirePermission("roles.manage_permissions")]
    public async Task<IActionResult> DeletePermission(Guid permissionId)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new DeletePermissionCommand(permissionId, userId)).ConfigureAwait(false));
    }

    [HttpGet("menus")]
    [RequirePermission("menus.view")]
    public async Task<IActionResult> SearchMenus([FromQuery] MenuSearchRequest request)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new SearchMenusQuery(request, userId)).ConfigureAwait(false));
    }

    [HttpGet("menus/navigation/{placement}")]
    public async Task<IActionResult> GetNavigationMenus(string placement)
    {
        var userId = GetActorId();
        var permissionKeys = User.Claims
            .Where(static claim => claim.Type == "permission")
            .Select(static claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var hasFullAccess = User.IsInRole("System Administrator");

        return HandleResponse(await sender.Send(new GetNavigationMenusQuery(placement, permissionKeys, userId, hasFullAccess)).ConfigureAwait(false));
    }

    [HttpGet("menus/{menuId:guid}")]
    [RequirePermission("menus.view")]
    public async Task<IActionResult> GetMenuById(Guid menuId)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new GetMenuByIdQuery(menuId, userId)).ConfigureAwait(false));
    }

    [HttpPost("menus")]
    [RequirePermission("menus.create")]
    public async Task<IActionResult> CreateMenu(CreateMenuRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetActorId();
        return HandleResponse(await sender.Send(new CreateMenuCommand(request, userId)).ConfigureAwait(false));
    }

    [HttpPut("menus/{menuId:guid}")]
    [RequirePermission("menus.edit")]
    public async Task<IActionResult> UpdateMenu(Guid menuId, UpdateMenuRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetActorId();
        return HandleResponse(await sender.Send(new UpdateMenuCommand(menuId, request with { Id = menuId }, userId)).ConfigureAwait(false));
    }

    [HttpDelete("menus/{menuId:guid}")]
    [RequirePermission("menus.delete")]
    public async Task<IActionResult> DeleteMenu(Guid menuId)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new DeleteMenuCommand(menuId, userId)).ConfigureAwait(false));
    }

    [HttpGet("user-devices")]
    [RequirePermission("users.view")]
    public async Task<IActionResult> GetUserDevices()
        => HandleResponse(await sender.Send(new GetAdminUserDevicesQuery()).ConfigureAwait(false));

    [HttpPatch("user-devices/{deviceId:guid}/revoke")]
    [RequirePermission("users.deactivate")]
    public async Task<IActionResult> RevokeUserDevice(Guid deviceId, RevokeUserDeviceRequest request)
    {
        var userId = GetActorId();
        return HandleResponse(await sender.Send(new RevokeUserDeviceCommand(deviceId, request, userId)).ConfigureAwait(false));
    }

    private string GetActorId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id was not found.");
        }

        return userId;
    }
}
