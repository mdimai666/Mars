using Microsoft.Extensions.Caching.Memory;

namespace Mars.SSO.Services;

internal class MemoryTokenCache : ITokenCache
{
    private readonly IMemoryCache _cache;

    public MemoryTokenCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string GetCacheKey(string token) => $"token:{token}";

    public Task CacheAsync(string externalToken, string localToken, TimeSpan ttl)
    {
        _cache.Set(GetCacheKey(externalToken), localToken, ttl);
        return Task.CompletedTask;
    }

    public Task<string?> GetLocalTokenAsync(string externalToken)
    {
        if (_cache.TryGetValue<string>(GetCacheKey(externalToken), out var localToken))
            return Task.FromResult<string?>(localToken);

        return Task.FromResult<string?>(null);
    }

    public Task InvalidateAsync(string token)
    {
        _cache.Remove(GetCacheKey(token));
        return Task.CompletedTask;
    }
}

public interface ITokenCache
{
    Task CacheAsync(string externalToken, string localToken, TimeSpan ttl);
    Task<string?> GetLocalTokenAsync(string externalToken);
    Task InvalidateAsync(string token);
}
