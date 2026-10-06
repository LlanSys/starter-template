using ST.Domain.Features.HR.Employees.Entities;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Features.Shared.Phone;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace ST.Application.Features.HR.Employees.Mappings;

public static class EmployeeMapping
{
    public static Employee ToEntity(this CreateEmployeeRequest request, string generatedNumber, string createdBy)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var phone = PhoneNumberFormatter.Normalize(
            request.CountryCode,
            request.PhoneNationalNumber,
            request.PhoneNumber);

        return Employee.Create(
            generatedNumber,
            request.Email,
            request.FirstName,
            request.LastName,
            request.IdNumber,
            phone.CountryCode,
            phone.NationalNumber,
            phone.E164,
            request.DepartmentId,
            request.ManagerId ?? Guid.Empty,
            createdBy
        );
    }

    public static EmployeeResponse ToEmployeeResponse(this Employee employee, string departmentName = "", string managerName = "")
    {
        ArgumentNullException.ThrowIfNull(employee, nameof(employee));

        return new(
            employee.Id,
            $"{employee.FirstName} {employee.LastName}",
            employee.Number,
            employee.Email,
            employee.IdNumber,
            employee.PhoneNumber,
            employee.DepartmentId,
            employee.ManagerId,
            departmentName,
            managerName,
            employee.CountryCode,
            employee.PhoneNationalNumber
        );
    }

    public static Expression<Func<Employee, EmployeeResponse>> AsResponse => employee =>
        new EmployeeResponse(
            employee.Id,
            $"{employee.FirstName} {employee.LastName}",
            employee.Number,
            employee.Email,
            employee.IdNumber,
            employee.PhoneNumber,
            employee.DepartmentId,
            employee.ManagerId,
            string.Empty,
            string.Empty,
            employee.CountryCode,
            employee.PhoneNationalNumber
        );

    public static List<EmployeeResponse> ToEmployeeResponseList(this Collection<Employee> employees) =>
        [.. employees.Select(e => e.ToEmployeeResponse())];

}
