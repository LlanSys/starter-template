namespace ST.SharedKernel.Features.HR.Departments.Dtos;

public sealed record DepartmentResponse(
    Guid Id,
    string Code,
    string Name,
    string Description,
    bool IsActive);
