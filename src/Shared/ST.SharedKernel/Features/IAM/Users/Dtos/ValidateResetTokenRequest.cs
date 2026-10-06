namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record ValidateResetTokenRequest(
    string Email,
    string Token);
