using System.ComponentModel.DataAnnotations;

namespace ST.Infrastructure.Configuration;

public sealed class KycDocumentSafetySettings
{
    public const string SectionName = "KycDocumentSafety";
    public string Provider { get; init; } = "ClamAv";
    [Required] public string Host { get; init; } = "localhost";
    [Range(1, 65535)] public int Port { get; init; } = 13315;
    public bool UseTls
    {
        get; init;
    }
    [Range(5, 120)] public int TimeoutSeconds { get; init; } = 30;
    [Range(150, 600)] public int LeaseSeconds { get; init; } = 180;
    [Range(1, 30)] public int PollSeconds { get; init; } = 5;
    [Range(1, 25)] public int BatchSize { get; init; } = 10;
    [Range(1, 10)] public int MaxAttempts { get; init; } = 5;
    [Range(1, 168)] public int MaxDefinitionsAgeHours { get; init; } = 72;
}
