using Mars.Cms.Contracts.Feedbacks;
using Mars.Contracts.Common;
using Mars.Data.Contexts;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Cms.Host.Kpi;

public class FeedbacksNewThisWeekKpiHandler : IKpiHandler
{
    public string Key => FeedbackKpiKeys.NewThisWeek;
    public string CacheKey => $"kpi:{FeedbackKpiKeys.NewThisWeek}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMarsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    public FeedbacksNewThisWeekKpiHandler(IMarsDbContextFactory factory, IMemoryCache cache, IEventManager eventManager)
    {
        _factory = factory;
        _cache = cache;

        eventManager.AddEventListener(eventManager.Defaults.FeedbackAdd(), _ => _cache.Remove(CacheKey));
        eventManager.AddEventListener(eventManager.Defaults.FeedbackDelete(), _ => _cache.Remove(CacheKey));
    }

    public Task<KpiResult> GetAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var now = DateTimeOffset.Now;
                var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
                var weekStart = new DateTimeOffset(now.Date.AddDays(-daysSinceMonday), now.Offset);
                using var context = _factory.CreateInstance();
                var totalCount = await context.Feedbacks.CountAsync(s => s.CreatedAt >= weekStart, cancellationToken);
                return new KpiResult(Key, totalCount, Key);
            })!;
    }
}
