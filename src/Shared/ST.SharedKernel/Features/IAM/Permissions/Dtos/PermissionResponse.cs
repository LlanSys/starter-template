namespace ST.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record PermissionResponse(
    Guid Id,
    Guid? DepartmentId,
    string Key,
    string Context,
    string Resource,
    string Action,
    string Description,
    bool IsActive);
