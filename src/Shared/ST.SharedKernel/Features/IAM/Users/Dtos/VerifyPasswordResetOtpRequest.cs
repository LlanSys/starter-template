namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record VerifyPasswordResetOtpRequest(string Email, string Code);
