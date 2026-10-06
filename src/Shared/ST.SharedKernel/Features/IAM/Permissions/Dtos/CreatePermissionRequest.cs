namespace ST.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record CreatePermissionRequest
{


    public required string Context { get; init; }

    public required string Resource { get; init; }

    public required string Action { get; init; }

    public string Description { get; init; } = string.Empty;
}
