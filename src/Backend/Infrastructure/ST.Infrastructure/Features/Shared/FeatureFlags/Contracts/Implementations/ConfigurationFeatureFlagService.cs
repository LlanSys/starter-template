using ST.Application.Features.Shared.FeatureFlags.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ST.Infrastructure.Features.Shared.FeatureFlags.Contracts.Implementations;

internal sealed class ConfigurationFeatureFlagService(IOptionsSnapshot<FeatureFlagSettings> options) : IFeatureFlagService
{
    public ValueTask<bool> IsEnabledAsync(string flagKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flagKey);

        var settings = options.Value;
        if (!settings.Provider.Equals("Configuration", StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(!settings.FailClosed);
        }

        return ValueTask.FromResult(
            settings.Flags.TryGetValue(flagKey.Trim(), out var enabled) && enabled);
    }
}
