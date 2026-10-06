namespace ST.Application.Features.Shared.FeatureFlags.Contracts.Interfaces;

public interface IFeatureFlagService
{
    ValueTask<bool> IsEnabledAsync(string flagKey, CancellationToken cancellationToken = default);
}
