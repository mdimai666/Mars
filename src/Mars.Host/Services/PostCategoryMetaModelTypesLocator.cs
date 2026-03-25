using System.Collections.Frozen;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.Extensions;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

internal sealed class PostCategoryMetaLocator : IPostCategoryMetaLocator, IMarsAppLifetimeService
{
    private sealed record CacheSnapshot(
        FrozenDictionary<string, PostCategoryTypeInfo> ByName,
        FrozenDictionary<Guid, string> IdToName
    );

    private volatile CacheSnapshot? _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;

    public PostCategoryMetaLocator(IServiceScopeFactory scopeFactory)
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
                (IPostCategoryTypeRepository repo) => repo.ListAllDetail(new(), ct));

            var idToName = types.ToFrozenDictionary(s => s.Id, s => s.TypeName);
            var byName = types.ToFrozenDictionary(
                s => s.TypeName,
                s => new PostCategoryTypeInfo { PostCategoryType = s });

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

    public PostCategoryTypeDetail? GetTypeDetailById(Guid id)
    {
        var cache = GetCacheSync();
        return cache.IdToName.TryGetValue(id, out var name)
            ? cache.ByName.GetValueOrDefault(name)?.PostCategoryType
            : null;
    }

    public PostCategoryTypeDetail? GetTypeDetailByName(string typeName)
        => GetCacheSync().ByName.GetValueOrDefault(typeName)?.PostCategoryType;

    public bool ExistType(Guid id)
        => GetCacheSync().IdToName.ContainsKey(id);

    public bool ExistType(string typeName)
        => GetCacheSync().ByName.ContainsKey(typeName);

    public IReadOnlyDictionary<string, PostCategoryTypeDetail> GetTypeDict()
        => GetCacheSync().ByName.ToDictionary(kv => kv.Key, kv => kv.Value.PostCategoryType);

    public void InvalidateCache()
        => _cache = null;

    [StartupOrder(10)]
    public Task OnStartupAsync()
    {
        _ = GetCacheAsync();
        return Task.CompletedTask;
    }
}

public sealed record PostCategoryTypeInfo
{
    public required PostCategoryTypeDetail PostCategoryType { get; init; }
}
