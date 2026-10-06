using System.ComponentModel.DataAnnotations;

namespace ST.UI.Blazor.Configuration;

internal sealed class SessionLifecycleSettings
{
    public const string SectionName = "SessionLifecycle";

    public bool Enabled { get; init; } = true;

    [Range(1, 1440)]
    public int IdleTimeoutMinutes { get; init; } = 10;

    [Range(1, 120)]
    public int WarningBeforeTimeoutMinutes { get; init; } = 2;

    [Range(1, 60)]
    public int PollIntervalSeconds { get; init; } = 15;

    [Range(1, 120)]
    public int KeepAliveIntervalMinutes { get; init; } = 5;

    [Required]
    public string SignInPath { get; init; } = "/iam/sign-in";
}
