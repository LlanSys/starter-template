namespace ST.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record UpdatePermissionRequest
{
    public string ModuleKey { get; init; } = "iam";
    public required Guid Id { get; init; }


    public required string Context { get; init; }

    public required string Resource { get; init; }

    public required string Action { get; init; }

    public string Description { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}
