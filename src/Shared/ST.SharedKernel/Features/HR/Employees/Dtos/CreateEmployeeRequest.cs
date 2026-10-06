using System;

namespace ST.SharedKernel.Features.HR.Employees.Dtos;

public record CreateEmployeeRequest
{
    // Required fields
    public required string IdNumber { get; set; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string CountryCode { get; init; } = "+254";
    public string PhoneNationalNumber { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public required DateOnly HireDate { get; init; }
    public required Guid DepartmentId { get; init; }
    public required Guid PositionId { get; init; }

    // Optional fields
    public DateOnly? DateOfBirth { get; init; }
    public Guid? ManagerId { get; init; }

}
