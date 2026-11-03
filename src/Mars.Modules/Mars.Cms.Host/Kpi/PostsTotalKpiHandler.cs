using Mars.Cms.Contracts.Posts;
using Mars.Contracts.Common;
using Mars.Data.Contexts;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Cms.Host.Kpi;

public class PostsTotalKpiHandler : IParameterizedKpiHandler
{
    public string Key => PostKpiKeys.Total;
    public string CacheKey => $"kpi:{PostKpiKeys.Total}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMarsDbContextFactory _factory;
    private readonly IMemoryCache _cache;

    public PostsTotalKpiHandler(IMarsDbContextFactory factory, IMemoryCache cache, IEventManager eventManager)
    {
        _factory = factory;
        _cache = cache;

        // события записей пер-тип (entity.post/{typeName}/add|delete) — сбрасываем глобальный ключ
        // и ключ типа из топика; wildcard-подписка в конструкторе (паттерн MetaValuesTotalKpiHandler)
        eventManager.AddEventListener(eventManager.Defaults.PostAnyOperation(), payload =>
        {
            _cache.Remove(CacheKey);

            var typeName = TypeNameFromTopic(payload.Topic);
            if (typeName is not null)
                _cache.Remove($"{CacheKey}:{typeName}");
        });
    }

    public Task<KpiResult> GetAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            CacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                using var context = _factory.CreateInstance();
                var totalCount = await context.Posts.CountAsync(cancellationToken);
                return new KpiResult(Key, totalCount, Key);
            })!;
    }

    public Task<KpiResult> GetAsync(string parameter, CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            $"{CacheKey}:{parameter}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                using var context = _factory.CreateInstance();
                var totalCount = await context.Posts.CountAsync(p => p.PostType!.TypeName == parameter, cancellationToken);
                return new KpiResult(PostKpiKeys.TotalFor(parameter), totalCount, Key);
            })!;
    }

    /// <summary>Тип записи из топика "entity.post/{typeName}/{operation}"</summary>
    static string? TypeNameFromTopic(string topic)
    {
        var parts = topic.Split('/');
        return parts.Length >= 3 ? parts[1] : null;
    }
}
