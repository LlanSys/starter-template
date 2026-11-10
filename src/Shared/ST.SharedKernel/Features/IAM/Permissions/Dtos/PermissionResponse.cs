namespace ST.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record PermissionResponse(
    Guid Id,

    string Key,
    string Context,
    string Resource,
    string Action,
    string Description,
    bool IsActive)
{
    public bool IsStandard { get; init; }
    public string ModuleKey { get; init; } = "iam";
}
