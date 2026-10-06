namespace ST.Domain.Shared.Contracts.Settings;

public interface IOrgSettingsProvider
{
    Task<string?> GetSettingAsync(string key, CancellationToken ct = default);
    Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default);
}
