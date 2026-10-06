namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AdminRoleListResponse(
    string Id,
    string Name,
    string NormalizedName,


    int UserCount);
