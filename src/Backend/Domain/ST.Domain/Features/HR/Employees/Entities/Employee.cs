using System;
using System.Collections.Generic;
using System.Text;

using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;

namespace ST.Domain.Features.HR.Employees.Entities;

public class Employee : BaseEntity, ISoftDeletable
{
    public string Number { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string IdNumber { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = "+254";
    public string PhoneNationalNumber { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public Guid DepartmentId { get; private set; }
    public Guid? ManagerId { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }


    private Employee() { } // EF Core

    public static Employee Create(
        string number,
        string email,
        string fName,
        string lName,
        string idNumber,
        string countryCode,
        string phoneNationalNumber,
        string phoneNumber,
        Guid departmentId,
        Guid? managerId,
        string createdBy) => new()
    {
        Id = Guid.CreateVersion7(),
        Number = number,
        Email = email,
        FirstName = fName,
        LastName = lName,
        IdNumber = idNumber,
        CountryCode = countryCode,
        PhoneNationalNumber = phoneNationalNumber,
        PhoneNumber = phoneNumber,
        DepartmentId = departmentId,
        ManagerId = managerId,
        CreatedBy = createdBy
    };

    public void Update(
        string number,
        string email,
        string firstName,
        string lastName,
        string idNumber,
        string countryCode,
        string phoneNationalNumber,
        string phoneNumber,
        Guid departmentId,
        Guid? managerId,
        string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(idNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNationalNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Number = number.Trim();
        Email = email.Trim();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        IdNumber = idNumber.Trim();
        CountryCode = countryCode.Trim();
        PhoneNationalNumber = phoneNationalNumber.Trim();
        PhoneNumber = phoneNumber.Trim();
        DepartmentId = departmentId;
        ManagerId = managerId;
        SetUpdatedInfo(updatedBy);
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedBy);

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        SetUpdatedInfo(deletedBy);
    }
}
