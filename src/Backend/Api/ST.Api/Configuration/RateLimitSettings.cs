namespace ST.Api.Configuration;

internal sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    public RateLimitPolicySettings LoginPolicy { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 120,
        QueueLimit = 3
    };

    public RateLimitPolicySettings AuthPolicy { get; set; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60,
        QueueLimit = 2
    };

    public RateLimitPolicySettings ApiPolicy { get; set; } = new()
    {
        PermitLimit = 100,
        WindowSeconds = 60,
        QueueLimit = 10
    };

    public RateLimitPolicySettings PasswordResetPolicy { get; set; } = new()
    {
        PermitLimit = 3,
        WindowSeconds = 900,
        QueueLimit = 0
    };

    public RateLimitPolicySettings TwoFactorPolicy { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 300,
        QueueLimit = 3
    };

    public RateLimitPolicySettings FileUploadPolicy { get; set; } = new()
    {
        PermitLimit = 20,
        WindowSeconds = 3600,
        QueueLimit = 5
    };

    public RateLimitPolicySettings RefreshTokenPolicy { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
        QueueLimit = 2
    };
}

internal sealed class RateLimitPolicySettings
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public int QueueLimit { get; set; }
}
