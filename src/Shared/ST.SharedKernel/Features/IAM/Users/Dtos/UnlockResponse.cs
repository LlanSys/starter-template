namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record UnlockResponse
{
    public bool Success { get; init; }
    public bool AccountLocked { get; init; }
    public bool SessionExpired { get; init; }
    public string? SessionId { get; init; }
}
