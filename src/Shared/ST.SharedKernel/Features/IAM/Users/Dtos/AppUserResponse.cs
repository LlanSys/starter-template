using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record AppUserResponse(
    string Id,
    string Username,
    string FirstName,
    string LastName,
    string FullName, // Calculated: $"{FirstName} {LastName}"
    string? PhoneNumber,
    string? IdNumber,
    string Email,
    string Gender,
    Uri? ProfilePictureUrl,

    bool IsActive,
    bool TwoFactorEnabled,
    bool RequirePasswordChange,

    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,

    ICollection<string> Roles,
    Guid TenantId,
    Guid? EmployeeId,
    Guid? CustomerId
);
