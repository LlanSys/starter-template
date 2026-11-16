using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ST.Infrastructure.Contracts.Implementations.Caching;

namespace ST.Tests.Integration.Caching;

public sealed class CacheFactoryScopeTests
{
    [Fact]
    public async Task CancellableCacheMissPreservesRequestExecutionContextAcrossAwaits()
    {
        using var services = Provider();
        var cache = Cache(services);
        var identity = new AsyncLocal<string?> { Value = "tenant-a/actor-a" };
        using var cancellation = new CancellationTokenSource();
        var result = await cache.GetOrCreateAsync("request-context", async token =>
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            return identity.Value;
        }, ct: cancellation.Token);

        Assert.Equal("tenant-a/actor-a", result);
    }

    [Fact]
    public async Task DirectReadReturnsStoredValueAndDoesNotPoisonAMissingEntry()
    {
        using var services = Provider();
        var cache = Cache(services);
        await cache.SetAsync("existing", "stored");
        Assert.Equal("stored", await cache.GetAsync<string>("existing"));
        Assert.Null(await cache.GetAsync<string>("missing"));
        Assert.Equal("created", await cache.GetOrCreateAsync("missing", _ => Task.FromResult("created")));
    }

    [Fact]
    public async Task ParallelTenantMissesAndHitsKeepTheirOwnLogicalIdentity()
    {
        using var services = Provider();
        var cache = Cache(services);
        var identity = new AsyncLocal<string?>();
        async Task Verify(string tenant)
        {
            identity.Value = tenant;
            using var cancellation = new CancellationTokenSource();
            var value = await cache.GetOrCreateAsync(tenant, async _ =>
            {
                await Task.Yield();
                return identity.Value;
            }, ct: cancellation.Token);

            Assert.Equal(tenant, value);
        }
        await Task.WhenAll(Enumerable.Range(0, 12).Select(i => Verify("tenant-" + i)));
        await Task.WhenAll(Enumerable.Range(0, 12).Select(i => Verify("tenant-" + i)));
    }

    [Fact]
    public async Task CanceledFactoryFinishesBeforeItsOwnerReturnsAndLiveWaiterRetries()
    {
        using var services = Provider();
        var cache = Cache(services);
        using var ownerCancellation = new CancellationTokenSource();
        using var waiterCancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowCleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = false;
        var owner = cache.GetOrCreateAsync("canceled-owner", async token =>
        {
            started.SetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return "unreachable";
            }
            finally
            {
                cleanupStarted.SetResult();
                await allowCleanup.Task;
                finished = true;
            }
        }, ct: ownerCancellation.Token);

        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var waiter = cache.GetOrCreateAsync("canceled-owner", _ => Task.FromResult("live-waiter"), ct: waiterCancellation.Token);
        ownerCancellation.Cancel();
        await cleanupStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(owner.IsCompleted);
        allowCleanup.SetResult();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => owner);
        Assert.True(finished);
        Assert.Equal("live-waiter", await waiter.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal("live-waiter", await cache.GetAsync<string>("canceled-owner"));
    }

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddHybridCache();
        return services.BuildServiceProvider();
    }

    private static HybridCacheService Cache(IServiceProvider services)
    {
        return new HybridCacheService(services.GetRequiredService<HybridCache>(), NullLogger<HybridCacheService>.Instance, TimeProvider.System);
    }
}
