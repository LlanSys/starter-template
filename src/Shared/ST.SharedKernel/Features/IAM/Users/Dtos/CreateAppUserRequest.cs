using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record CreateAppUserRequest(
    [Required, StringLength(50)] string Username,
    [Required, EmailAddress] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    [Required, StringLength(50)] string FirstName,
    [Required, StringLength(50)] string LastName,

    [Phone] string? PhoneNumber,
    string? IdNumber,
    string Gender,

    Guid? EmployeeId,
    Guid? CustomerId,

    ICollection<string>? Roles
);
