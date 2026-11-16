# Tenant cache execution and invalidation

The local two-customer acceptance run exposed a cache-miss scope failure: a customer setting was written under the correct tenant, but a queued HybridCache factory lost the request ExecutionContext and resolved the reserved platform workspace. The same risk applies to other ambient actor/request context used by a factory. This was a release-blocking defect, not a passed isolation check.

HybridCacheService now captures and restores the caller's execution context around factory invocation. It awaits the owning scoped factory through cleanup instead of detaching it when the request is cancelled. The original cancellation token still reaches factory I/O. A live coalesced waiter retries if the previous factory owner was cancelled. Never capture a request service in a fire-and-forget cache operation.

GetAsync performs a real lookup without caching a missing value. Version sentinels bypass each replica's local read cache and use the shared distributed store. Response entries retain tenant and optional user scope; mutations replace the common tenant epoch, invalidating every user's versioned results. Epochs include a unique identifier as well as injected time. Failed AppResponse outcomes do not invalidate successful reads.

The request-v2 namespace retires results created by the earlier implementation. Deploy all API replicas together; drain old instances before accepting traffic. Do not flush Redis or delete the Data Protection key ring to upgrade this cache. Old response entries expire under their existing TTL. Redis is required for cross-replica epochs; a process-local memory provider only coordinates its own process.

Regression coverage uses the real HybridCache implementation for execution context across awaits, parallel tenant misses/hits, direct reads and cancellation cleanup/coalescing. A real password-and-authenticator HTTP test creates the same key in the platform workspace and two customer workspaces, warms list/single reads, updates A, rejects a cross-tenant write and deletes A while B remains unchanged. The platform decoy must never appear in either customer's results.

Reference: [.NET 10 HybridCache factory dispatch](https://github.com/dotnet/extensions/blob/release/10.0/src/Libraries/Microsoft.Extensions.Caching.Hybrid/Internal/DefaultHybridCache.StampedeStateT.cs).
