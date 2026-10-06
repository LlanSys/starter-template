namespace ST.SharedKernel.Features.IAM.Users.Dtos;


public record UnlockRequest(string Password, string? Email = null);