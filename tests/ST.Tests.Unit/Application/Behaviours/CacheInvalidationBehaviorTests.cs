using ST.Application.Behaviours;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Shared.Contracts.Common;
using MediatR;
using ST.SharedKernel.Dtos.Common;
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
        Assert.Contains("lookups:version:global", cache.SetKeys);
        Assert.Contains($"lookups:version:tenant:{TenantId:D}", cache.SetKeys);
        Assert.DoesNotContain($"lookups:versiontenant:{TenantId:D}", cache.SetKeys);
        Assert.Contains(CacheKeys.Request($"lookups:version:tenant:{TenantId:D}"), cache.SetKeys);
        var first = cache.Values[CacheKeys.Request($"lookups:version:tenant:{TenantId:D}")];
        await behavior.Handle(request, _ => Task.FromResult("ok"), CancellationToken.None);
        Assert.NotEqual(first, cache.Values[CacheKeys.Request($"lookups:version:tenant:{TenantId:D}")]);
    }

    [Fact]
    public async Task ExpectedBusinessFailureDoesNotInvalidatePreviouslySuccessfulReads()
    {
        var cache = new RecordingCacheService();
        var behavior = new CacheInvalidationBehavior<RejectedMutation, AppResponse<bool>>(
            cache,
            new FixedTenantProvider(TenantId),
            NullLogger<CacheInvalidationBehavior<RejectedMutation, AppResponse<bool>>>.Instance,
            TimeProvider.System);

        var rejected = new AppResponse<bool> { IsSuccess = false, Message = "Rejected" };
        var result = await behavior.Handle(new RejectedMutation(), _ => Task.FromResult(rejected), CancellationToken.None);
        Assert.Same(rejected, result);
        Assert.Empty(cache.SetKeys);
        Assert.Empty(cache.RemovedKeys);
    }

    private sealed record RejectedMutation : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest
    {
        public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("lookups")];
        public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("lookups", "one")];
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
        public Dictionary<string, object?> Values { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            return Task.FromResult(default(T));
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            return Task.FromResult(false);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            SetKeys.Add(key);
            Values[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            RemovedKeys.Add(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? expiration = null,
            CancellationToken ct = default)
        {
            return await factory(ct);
        }
    }
}
