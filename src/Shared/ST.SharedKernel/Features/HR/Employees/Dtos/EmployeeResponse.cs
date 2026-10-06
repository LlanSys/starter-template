using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.HR.Employees.Dtos;

public record EmployeeResponse(
    Guid Id,
    string FullName,
    string Number,
    string Email,
    string IdNumber,
    string PhoneNumber,
    Guid DepartmentId,
    Guid? ManagerId,
    string DepartmentName = "",
    string ManagerName = "",
    string CountryCode = "+254",
    string PhoneNationalNumber = ""
);
