using System.Threading.Tasks;

namespace Shadow.FastEndpoints.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null);
    Task RemoveAsync(string key);
    /// <summary>
    /// Try get from cache or use the factory to create the value and set cache. Uses an optional refresh lock to avoid stampede.
    /// </summary>
    Task<T?> TryGetOrRefreshAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? ttl = null, string? refreshLockKey = null, TimeSpan? lockTimeout = null);
}
