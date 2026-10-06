namespace ST.SharedKernel.Features.IAM.Users.Dtos;
public record UserClaimsResponse
{
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string ValueType { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string OriginalIssuer { get; init; } = string.Empty;
    public Dictionary<string, string> Properties { get; init; } = new();
}