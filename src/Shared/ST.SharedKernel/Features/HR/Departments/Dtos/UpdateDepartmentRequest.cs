namespace ST.SharedKernel.Features.HR.Departments.Dtos;

public sealed record UpdateDepartmentRequest
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}
