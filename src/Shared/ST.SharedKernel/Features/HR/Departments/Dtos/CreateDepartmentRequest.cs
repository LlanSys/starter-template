namespace ST.SharedKernel.Features.HR.Departments.Dtos;

public sealed record CreateDepartmentRequest
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;
}
