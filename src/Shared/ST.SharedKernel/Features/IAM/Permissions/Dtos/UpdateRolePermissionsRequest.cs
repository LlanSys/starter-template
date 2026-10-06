namespace ST.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> PermissionKeys);
