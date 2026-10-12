using Mars.Contracts.Common;
using Mars.Data.Contexts;
using Mars.Media.Contracts.Files;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Media.Host.Kpi;

public class MediaFilesTotalKpiHandler : IKpiHandler
{
    public string Key => MediaKpiKeys.Files;
    public string CacheKey => $"kpi:{MediaKpiKeys.Files}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMarsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    public MediaFilesTotalKpiHandler(IMarsDbContextFactory factory, IMemoryCache cache, IEventManager eventManager)
    {
        _factory = factory;
        _cache = cache;

        eventManager.AddEventListener(eventManager.Defaults.MediaFileAdd(), _ => _cache.Remove(CacheKey));
        eventManager.AddEventListener(eventManager.Defaults.MediaFileDelete(), _ => _cache.Remove(CacheKey));
    }

    public Task<KpiResult> GetAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                using var context = _factory.CreateInstance();
                var totalCount = await context.Files.CountAsync(cancellationToken);
                return new KpiResult(Key, totalCount, Key);
            })!;
    }
}
