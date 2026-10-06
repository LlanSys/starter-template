namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record UpdateAdminUserRequest(
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? IdNumber,
    string Gender,
    IReadOnlyList<string> Roles);
