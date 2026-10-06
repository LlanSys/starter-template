namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record UserRolesResponse(string UserId, string UserName, IReadOnlyList<string> Roles);
