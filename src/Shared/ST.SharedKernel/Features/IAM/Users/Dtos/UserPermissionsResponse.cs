namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record UserPermissionsResponse(string UserId, string UserName, IReadOnlyList<string> PermissionKeys);
