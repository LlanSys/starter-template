namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record VerifyPasswordRequest(
    string UserId,
    string Email,
    string Password
);
