using System.ComponentModel.DataAnnotations;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record CreateAppUserRequest(
    [Required, StringLength(50)] string Username,
    [Required, EmailAddress] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    [Required, StringLength(50)] string FirstName,
    [Required, StringLength(50)] string LastName,
    [Phone] string? PhoneNumber,
    ICollection<string>? Roles);
