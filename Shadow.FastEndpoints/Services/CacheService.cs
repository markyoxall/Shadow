using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Shadow.FastEndpoints.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IConnectionMultiplexer? _redis;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public CacheService(IDistributedCache cache, IConnectionMultiplexer? redis = null)
    {
        _cache = cache;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var bytes = await _cache.GetAsync(key);
        if (bytes == null) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
        }
        catch
        {
            // If deserialization fails, remove key to avoid repeated errors
            await _cache.RemoveAsync(key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
        var options = new DistributedCacheEntryOptions();
        if (absoluteExpirationRelativeToNow.HasValue)
            options.SetAbsoluteExpiration(absoluteExpirationRelativeToNow.Value);
        else
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

        await _cache.SetAsync(key, bytes, options);
    }

    public Task RemoveAsync(string key) => _cache.RemoveAsync(key);

    public async Task<T?> TryGetOrRefreshAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? ttl = null, string? refreshLockKey = null, TimeSpan? lockTimeout = null)
    {
        // Try normal get
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        // Determine lock key
        refreshLockKey ??= $"refresh:{key}";
        lockTimeout ??= TimeSpan.FromSeconds(10);

        // If Redis available, try a simple SET NX with expiry as lock
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var lockValue = Guid.NewGuid().ToString("N");
            var gotLock = await db.StringSetAsync(refreshLockKey, lockValue, lockTimeout, When.NotExists);
            if (gotLock)
            {
                try
                {
                    var result = await factory();
                    if (result != null)
                        await SetAsync(key, result, ttl);
                    return result;
                }
                finally
                {
                    // Release lock only if value matches
                    var script = @"if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
                    await db.ScriptEvaluateAsync(script, new RedisKey[] { refreshLockKey }, new RedisValue[] { lockValue });
                }
            }
            else
            {
                // Wait briefly for the other refresher to populate cache
                await Task.Delay(250);
                return await GetAsync<T>(key);
            }
        }

        // Fallback to in-memory semaphore lock
        var sem = _locks.GetOrAdd(refreshLockKey, _ => new SemaphoreSlim(1, 1));
        if (await sem.WaitAsync(0))
        {
            try
            {
                var result = await factory();
                if (result != null)
                    await SetAsync(key, result, ttl);
                return result;
            }
            finally
            {
                sem.Release();
            }
        }
        else
        {
            await Task.Delay(250);
            return await GetAsync<T>(key);
        }
    }
}
