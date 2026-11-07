using ST.Application.Behaviours;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Shared.Contracts.Common;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace ST.Tests.Unit.Application.Behaviours;

public sealed class CacheInvalidationBehaviorTests
{
    private static readonly Guid TenantId = Guid.Parse("0194f700-0000-7000-8000-000000000001");

    [Fact]
    public async Task Handle_Should_Bump_Global_And_Tenant_Scoped_Version_Keys()
    {
        var cache = new RecordingCacheService();
        var behavior = new CacheInvalidationBehavior<TestInvalidationRequest, string>(
            cache,
            new FixedTenantProvider(TenantId),
            NullLogger<CacheInvalidationBehavior<TestInvalidationRequest, string>>.Instance,
            TimeProvider.System);

        var request = new TestInvalidationRequest([CacheKeys.GroupVersion("lookups")]);

        var result = await behavior.Handle(request, _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Contains("lookups:version:global", cache.RemovedKeys);
        Assert.Contains($"lookups:version:tenant:{TenantId:D}", cache.RemovedKeys);
        Assert.DoesNotContain($"lookups:versiontenant:{TenantId:D}", cache.RemovedKeys);
    }

    private sealed record TestInvalidationRequest(IReadOnlyList<string> GroupVersionKeysToInvalidate)
        : IRequest<string>, ICacheInvalidatorRequest;

    private sealed class FixedTenantProvider(Guid tenantId) : ICurrentTenantProvider
    {
        public Guid TenantId { get; } = tenantId;
    }

    private sealed class RecordingCacheService : ICacheService
    {
        public List<string> SetKeys { get; } = [];
        public List<string> RemovedKeys { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
            => Task.FromResult(default(T));

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            SetKeys.Add(key);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
            => Task.CompletedTask;

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? expiration = null,
            CancellationToken ct = default)
            => await factory(ct);
    }
}
