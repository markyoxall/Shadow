# Redis caching, Polly, and Data API Builder - Guidance for Shadow project

This document explains how Redis caching is applied in the `Shadow.FastEndpoints` project, how to use Polly for resilience, a simple Redis refresh lock to avoid cache stampede, and notes on Microsoft Data API Builder.

## What was implemented
- `ICacheService` / `CacheService` wrapper around `IDistributedCache` with JSON serialization, TTL, and deserialization failure handling.
- Redis is used when `ConnectionStrings:Redis` is configured; otherwise an in-memory distributed cache is used as a fallback.
- `CacheService.TryGetOrRefreshAsync` implements a cache-aside read with an optional refresh lock. If Redis is available the lock uses a simple `SET NX` pattern with expiry. Otherwise it falls back to an in-memory `SemaphoreSlim` per key.
- `NotesEndpoint` was updated to use the cache-aside pattern and to evict the `notes:{id}` key on writes.
- Polly policies are registered for an example named `ResilientClient` HttpClient with retry and circuit-breaker policies.

## Configuration
- Add Redis connection string to `appsettings.Development.json` or environment secrets:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

- When Redis is configured, the app will register `IConnectionMultiplexer` for optional lock operations and `IDistributedCache` backed by StackExchange.Redis. If not set, `DistributedMemoryCache` is used.

## How the cache works (Notes example)
- GET `/notes/{id}`:
  - Try `ICacheService.GetAsync<Note>("notes:{id}")`.
  - If miss, `CacheService.TryGetOrRefreshAsync` will attempt to acquire a refresh lock and call the provided factory (in `NotesEndpoint` this is the Orleans grain `GetAsync`).
  - The result is cached with a default TTL (10 minutes) or specified TTL.
- POST `/notes/{id}`:
  - Write via Orleans grain `SetAsync` and then evict the `notes:{id}` cache key.

## Redis refresh lock details
- When Redis is present, the refresh lock uses `StringSetAsync(key, value, expiry, When.NotExists)` to obtain a lock.
- After the factory completes, a Lua script is used to release the lock only if the lock value matches (safe release).
- If the lock is already held by another process, the caller waits briefly and then reads the cache again.
- This is a simple approach suitable for many cases. For stronger requirements consider RedLock or a mature distributed lock library.

## Polly usage
- Registered a named `HttpClient` called `ResilientClient` with:
  - Retry with fixed delays: 200ms, 1s, 2s (use exponential with jitter for production).
  - Circuit breaker: break after 5 consecutive failures for 30 seconds.
- Use via `IHttpClientFactory.CreateClient("ResilientClient")`.
- For service-to-service calls (BFF -> FastEndpoints or external APIs) create typed clients and tune policies per endpoint.

## Data API Builder (DAB)
- Microsoft Data API Builder can automatically expose REST/GraphQL endpoints for supported databases with declarative authorization rules.
- DAB is useful for quick CRUD surfaces or prototyping, but it bypasses your Orleans grain logic and domain invariants.
- Recommendation: Do not use DAB for Notes backed by Orleans grains. You could use DAB for other simple tables where you want rapid REST endpoints without domain logic.

## Examples
- Using the resilient HttpClient

```csharp
var client = httpClientFactory.CreateClient("ResilientClient");
var res = await client.GetAsync("https://api.example.com/data");
res.EnsureSuccessStatusCode();
```

- Using cache TryGetOrRefreshAsync (pattern)

```csharp
var note = await cache.TryGetOrRefreshAsync<Note>(
    key: $"notes:{id}",
    factory: async () => await grain.GetAsync(),
    ttl: TimeSpan.FromMinutes(10),
    refreshLockKey: $"refresh:notes:{id}");
```

## Further improvements
- Add metrics for cache hits/misses and lock contention.
- Use a stronger distributed lock (RedLock) if multi-datacenter or stricter guarantees required.
- Use jittered exponential backoff for Polly retry and tune policies per external service.
- Add a BFF-level composed cache for aggregated responses.

## Where to look in code
- `Shadow.FastEndpoints/Services/ICacheService.cs`
- `Shadow.FastEndpoints/Services/CacheService.cs`
- `Shadow.FastEndpoints/Endpoints/NotesEndpoint.cs`
- `Shadow.FastEndpoints/Program.cs`


