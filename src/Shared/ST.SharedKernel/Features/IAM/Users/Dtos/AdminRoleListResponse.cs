namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AdminRoleListResponse(
    string Id,
    string Name,
    string NormalizedName,
    Guid? DepartmentId,
    string DepartmentName,
    int UserCount);
