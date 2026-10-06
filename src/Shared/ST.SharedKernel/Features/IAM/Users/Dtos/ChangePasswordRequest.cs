namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
