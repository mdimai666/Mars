using System.Collections.Frozen;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Host.Shared.Extensions;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

internal sealed class UserMetaLocator : IUserMetaLocator
{
    private sealed record CacheSnapshot(
        FrozenDictionary<string, UserTypeInfo> ByName,
        FrozenDictionary<Guid, string> IdToName
    );

    private volatile CacheSnapshot? _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;

    public UserMetaLocator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    private async ValueTask<CacheSnapshot> GetCacheAsync(CancellationToken ct = default)
    {
        if (_cache is { } snapshot)
            return snapshot;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cache is { } cached)
                return cached;

            var types = await _scopeFactory.ExecuteScopedAsync(
                (IUserTypeRepository repo) => repo.ListAllDetail(new(), ct));

            var idToName = types.ToFrozenDictionary(s => s.Id, s => s.TypeName);
            var byName = types.ToFrozenDictionary(
                s => s.TypeName,
                s => new UserTypeInfo { UserType = s });

            _cache = new CacheSnapshot(byName, idToName);
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    private CacheSnapshot GetCacheSync()
        => _cache ?? GetCacheAsync().AsTask().GetAwaiter().GetResult();

    public UserTypeDetail? GetTypeDetailById(Guid id)
    {
        var cache = GetCacheSync();
        return cache.IdToName.TryGetValue(id, out var name)
            ? cache.ByName.GetValueOrDefault(name)?.UserType
            : null;
    }

    public UserTypeDetail? GetTypeDetailByName(string userTypeName)
        => GetCacheSync().ByName.GetValueOrDefault(userTypeName)?.UserType;

    public bool ExistType(Guid id)
        => GetCacheSync().IdToName.ContainsKey(id);

    public bool ExistType(string userTypeName)
        => GetCacheSync().ByName.ContainsKey(userTypeName);

    public IReadOnlyDictionary<string, UserTypeDetail> GetTypeDict()
        => GetCacheSync().ByName.ToDictionary(kv => kv.Key, kv => kv.Value.UserType);

    public async Task WarmUpAsync(CancellationToken ct = default)
        => await GetCacheAsync(ct);

    public void InvalidateCache()
        => _cache = null;
}

public sealed record UserTypeInfo
{
    public required UserTypeDetail UserType { get; init; }
}
