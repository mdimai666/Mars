using Mars.Contracts.Common;
using Mars.Data.Contexts;
using Mars.Identity.Contracts.Users;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Identity.Host.Kpi;

public class UsersNewThisMonthKpiHandler : IKpiHandler
{
    public string Key => UserKpiKeys.NewThisMonth;
    public string CacheKey => $"kpi:{UserKpiKeys.NewThisMonth}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMarsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    public UsersNewThisMonthKpiHandler(IMarsDbContextFactory factory, IMemoryCache cache, IEventManager eventManager)
    {
        _factory = factory;
        _cache = cache;

        eventManager.AddEventListener(eventManager.Defaults.UserAdd(), _ => _cache.Remove(CacheKey));
        eventManager.AddEventListener(eventManager.Defaults.UserDelete(), _ => _cache.Remove(CacheKey));
    }

    public Task<KpiResult> GetAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var now = DateTimeOffset.Now;
                var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
                using var context = _factory.CreateInstance();
                var totalCount = await context.Users.CountAsync(s => s.CreatedAt >= monthStart, cancellationToken);
                return new KpiResult(Key, totalCount, Key);
            })!;
    }
}
