using Mars.Cms.Contracts.PostTypes;
using Mars.Contracts.Common;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Cms.Host.Kpi;

public class PostTypesComponentKpiHandler : IKpiHandler
{
    public string Key => PostTypeKpiKeys.ComponentTypes;
    public string CacheKey => $"kpi:{PostTypeKpiKeys.ComponentTypes}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMarsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    public PostTypesComponentKpiHandler(IMarsDbContextFactory factory, IMemoryCache cache, IEventManager eventManager)
    {
        _factory = factory;
        _cache = cache;

        eventManager.AddEventListener(eventManager.Defaults.PostTypeAnyOperation(), _ => _cache.Remove(CacheKey));
    }

    public Task<KpiResult> GetAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                using var context = _factory.CreateInstance();
                var totalCount = await context.PostTypes
                    .CountAsync(s => s.Visibility == EPostTypeVisibility.Component, cancellationToken);
                return new KpiResult(Key, totalCount, Key);
            })!;
    }
}
