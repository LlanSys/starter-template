namespace ST.Infrastructure.Configuration;

public sealed class SessionSettings
{
    public const string SectionName = "SecuritySettings:SessionSettings";

    public int MaxConcurrentSessions { get; set; } = 1;
    public int SessionTimeoutMinutes { get; set; } = 15;
    public bool SlidingExpiration { get; set; } = true;
    public int CleanupIntervalMinutes { get; set; } = 30;
    public bool EnableConcurrentSessionNotification { get; set; } = true;
    public bool UseLockScreen { get; set; } = true;
}
