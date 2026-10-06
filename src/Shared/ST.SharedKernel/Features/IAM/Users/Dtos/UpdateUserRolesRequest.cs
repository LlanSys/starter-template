namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record UpdateUserRolesRequest(IReadOnlyList<string> Roles);
