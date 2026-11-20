using Mars.Cms.Contracts.Feedbacks;
using Mars.Contracts.Common;
using Mars.Data.Contexts;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Cms.Host.Kpi;

public class FeedbacksUnreadKpiHandler : IKpiHandler
{
    public string Key => FeedbackKpiKeys.Unread;
    public string CacheKey => $"kpi:{FeedbackKpiKeys.Unread}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMarsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    public FeedbacksUnreadKpiHandler(IMarsDbContextFactory factory, IMemoryCache cache, IEventManager eventManager)
    {
        _factory = factory;
        _cache = cache;

        eventManager.AddEventListener(eventManager.Defaults.FeedbackAdd(), _ => _cache.Remove(CacheKey));
        eventManager.AddEventListener(eventManager.Defaults.FeedbackDelete(), _ => _cache.Remove(CacheKey));
        eventManager.AddEventListener(eventManager.Defaults.FeedbackRead(), _ => _cache.Remove(CacheKey));
    }

    public Task<KpiResult> GetAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                using var context = _factory.CreateInstance();
                var unreadCount = await context.Feedbacks.CountAsync(s => !s.IsRead, cancellationToken);
                return new KpiResult(Key, unreadCount, Key);
            })!;
    }
}