namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record ConfirmUserEmailRequest(string Email, string Token);
