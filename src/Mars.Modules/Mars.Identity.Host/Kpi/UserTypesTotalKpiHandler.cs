using Mars.Contracts.Common;
using Mars.Data.Contexts;
using Mars.Identity.Contracts.UserTypes;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Identity.Host.Kpi;

public class UserTypesTotalKpiHandler : IKpiHandler
{
    public string Key => UserTypeKpiKeys.Total;
    public string CacheKey => $"kpi:{UserTypeKpiKeys.Total}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMarsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    public UserTypesTotalKpiHandler(IMarsDbContextFactory factory, IMemoryCache cache, IEventManager eventManager)
    {
        _factory = factory;
        _cache = cache;

        eventManager.AddEventListener(eventManager.Defaults.UserTypeAnyOperation(), _ => _cache.Remove(CacheKey));
    }

    public Task<KpiResult> GetAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                using var context = _factory.CreateInstance();
                var totalCount = await context.UserTypes.CountAsync(cancellationToken);
                return new KpiResult(Key, totalCount, Key);
            })!;
    }
}
