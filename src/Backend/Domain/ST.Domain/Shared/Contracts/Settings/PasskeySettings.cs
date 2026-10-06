namespace ST.Domain.Shared.Contracts.Settings;

public class PasskeySettings
{
    public const string SectionName = "Passkeys";

    public bool Enabled { get; set; } = false;
    public string ServerDomain { get; set; } = string.Empty;
    public string ServerName { get; set; } = "StarterTemplate";
    public string[] Origins { get; set; } = [];
}
