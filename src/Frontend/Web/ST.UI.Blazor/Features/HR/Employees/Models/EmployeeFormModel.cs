using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Features.Shared.Phone;

namespace ST.UI.Blazor.Features.HR.Employees.Models;

internal sealed class EmployeeFormModel
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string IdNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = PhoneNumberFormatter.DefaultCountryCode;

    public string PhoneNationalNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public Guid? ManagerId { get; set; }

    public DateTime? HireDate { get; set; } = DateTime.Today;

    public Guid PositionId { get; set; } = Guid.CreateVersion7();

    public static EmployeeFormModel From(EmployeeResponse employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        var names = employee.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return new EmployeeFormModel
        {
            Id = employee.Id,
            Number = employee.Number,
            FirstName = names.Length > 0 ? names[0] : employee.FullName,
            LastName = names.Length > 1 ? names[1] : string.Empty,
            Email = employee.Email,
            IdNumber = employee.IdNumber,
            CountryCode = employee.CountryCode,
            PhoneNationalNumber = employee.PhoneNationalNumber,
            PhoneNumber = employee.PhoneNumber,
            DepartmentId = employee.DepartmentId,
            ManagerId = employee.ManagerId
        };
    }

    public CreateEmployeeRequest ToCreateRequest() => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        IdNumber = IdNumber,
        CountryCode = CountryCode,
        PhoneNationalNumber = PhoneNationalNumber,
        PhoneNumber = PhoneNumber,
        DepartmentId = DepartmentId,
        ManagerId = ManagerId,
        HireDate = DateOnly.FromDateTime(HireDate ?? DateTime.Today),
        PositionId = PositionId
    };

    public UpdateEmployeeRequest ToUpdateRequest() => new()
    {
        Id = Id,
        Number = Number,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        IdNumber = IdNumber,
        CountryCode = CountryCode,
        PhoneNationalNumber = PhoneNationalNumber,
        PhoneNumber = PhoneNumber,
        DepartmentId = DepartmentId,
        ManagerId = ManagerId
    };
}
