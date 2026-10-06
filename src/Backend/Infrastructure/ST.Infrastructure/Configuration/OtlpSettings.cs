namespace ST.Infrastructure.Configuration;

public sealed class OtlpSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string Headers { get; set; } = string.Empty;
}
