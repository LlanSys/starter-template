namespace ST.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record UpdatePermissionRequest
{
    public required Guid Id { get; init; }

    public Guid? DepartmentId { get; init; }

    public required string Context { get; init; }

    public required string Resource { get; init; }

    public required string Action { get; init; }

    public string Description { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}
