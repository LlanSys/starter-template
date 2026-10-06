namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record ResetPasswordRequest(
    string Email,
    string? NewPassword,
    string? Password,
    string? ConfirmPassword,
    string? Token = null);
