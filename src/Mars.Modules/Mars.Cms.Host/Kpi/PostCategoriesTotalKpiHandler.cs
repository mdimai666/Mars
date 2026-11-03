using System.Collections.Concurrent;
using Mars.Cms.Contracts.PostCategories;
using Mars.Contracts.Common;
using Mars.Data.Contexts;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Cms.Host.Kpi;

public class PostCategoriesTotalKpiHandler : IParameterizedKpiHandler
{
    public string Key => PostCategoryKpiKeys.Total;
    public string CacheKey => $"kpi:{PostCategoryKpiKeys.Total}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMarsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    // события категорий без типа — для точечного сброса per-type ключей помним запрошенные параметры
    private readonly ConcurrentDictionary<string, byte> _cachedParameters = new();

    public PostCategoriesTotalKpiHandler(IMarsDbContextFactory factory, IMemoryCache cache, IEventManager eventManager)
    {
        _factory = factory;
        _cache = cache;

        eventManager.AddEventListener(eventManager.Defaults.PostCategoryAdd(), _ => Invalidate());
        eventManager.AddEventListener(eventManager.Defaults.PostCategoryDelete(), _ => Invalidate());
    }

    void Invalidate()
    {
        _cache.Remove(CacheKey);
        foreach (var parameter in _cachedParameters.Keys)
            _cache.Remove($"{CacheKey}:{parameter}");
    }

    public Task<KpiResult> GetAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                using var context = _factory.CreateInstance();
                var totalCount = await context.PostCategories.CountAsync(cancellationToken);
                return new KpiResult(Key, totalCount, Key);
            })!;
    }

    public Task<KpiResult> GetAsync(string parameter, CancellationToken cancellationToken)
    {
        _cachedParameters[parameter] = 0;
        return _cache.GetOrCreateAsync(
            $"{CacheKey}:{parameter}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                using var context = _factory.CreateInstance();
                var totalCount = await context.PostCategories.CountAsync(c => c.PostType!.TypeName == parameter, cancellationToken);
                return new KpiResult(PostCategoryKpiKeys.TotalFor(parameter), totalCount, Key);
            })!;
    }
}
