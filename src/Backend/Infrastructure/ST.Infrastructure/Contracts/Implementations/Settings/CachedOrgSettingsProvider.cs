using System.Text.Json;
using ST.Infrastructure.Logging;
using ST.Domain.Features.Shared.Contracts;
using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Contracts.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace ST.Infrastructure.Contracts.Implementations.Settings;

public class CachedOrgSettingsProvider : IOrgSettingsProvider
{
    private readonly IDistributedCache _cache;
    private readonly ICurrentTenantProvider _tenantProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<CachedOrgSettingsProvider> _logger;

    public CachedOrgSettingsProvider(
        IDistributedCache cache,
        ICurrentTenantProvider tenantProvider,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IEncryptionService encryptionService,
        ILogger<CachedOrgSettingsProvider> logger)
    {
        _cache = cache;
        _tenantProvider = tenantProvider;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<string?> GetSettingAsync(string key, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId;
        if (tenantId == Guid.Empty)
        {
            // If no tenant context, fallback to global appsettings
            return _configuration[key];
        }

        var cacheKey = $"tenant:{tenantId}:settings:{key}";

        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey, ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                return cachedValue;
            }
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogReadOrgSettingCacheWarning(_logger, ex);
        }

        // Cache miss, read from DB
        string? plainTextValue = null;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<ISharedUnitOfWork>();
            var setting = await unitOfWork.OrgSettingRepository.FirstOrDefaultAsync(x => x.Key == key, ct).ConfigureAwait(false);

            if (setting != null)
            {
                try
                {
                    plainTextValue = _encryptionService.Decrypt(setting.Value);
                }
                catch
                {
                    plainTextValue = setting.Value;
                }
            }
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogReadOrgSettingDatabaseError(_logger, ex);
        }

        // Hierarchy Fallback: if not in DB, fallback to appsettings
        if (string.IsNullOrEmpty(plainTextValue))
        {
            plainTextValue = _configuration[key];
        }

        // Cache it (even if null, cache empty string to prevent repeated DB hits)
        if (plainTextValue != null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };
                await _cache.SetStringAsync(cacheKey, plainTextValue, options, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ServiceLogDefinitions.LogWriteOrgSettingCacheWarning(_logger, ex);
            }
        }

        return plainTextValue;
    }

    public async Task<T?> GetSettingAsync<T>(string key, CancellationToken ct = default)
    {
        var stringValue = await GetSettingAsync(key, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(stringValue))
        {
            return default;
        }

        try
        {
            // If T is a primitive type or string, try to convert directly
            if (typeof(T) == typeof(string))
            {
                return (T)(object)stringValue;
            }

            if (typeof(T) == typeof(Guid))
            {
                return (T)(object)Guid.Parse(stringValue);
            }

            if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal))
            {
                return (T)Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);
            }

            // Otherwise assume JSON
            return JsonSerializer.Deserialize<T>(stringValue);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogDeserializeOrgSettingError(_logger, key, typeof(T).Name, ex);
            return default;
        }
    }
}
