namespace ST.SharedKernel.Features.HR.Employees.Dtos;

public sealed record UpdateEmployeeRequest
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Email { get; init; }

    public required string IdNumber { get; init; }

    public string CountryCode { get; init; } = "+254";

    public string PhoneNationalNumber { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public required Guid DepartmentId { get; init; }

    public Guid? ManagerId { get; init; }
}
