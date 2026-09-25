using Mars.Identity.Abstractions.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Identity.Host.Services;

public class SecurityStampCache(IMemoryCache cache) : ISecurityStampCache
{
    // после истечения TTL устаревшие куки покроет штатная сверка с БД (ValidationInterval)
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private static string Key(Guid userId) => $"security-stamp:{userId}";

    public void Mark(Guid userId, string newSecurityStamp)
        => cache.Set(Key(userId), newSecurityStamp, Ttl);

    public bool TryGetNewStamp(Guid userId, out string newSecurityStamp)
        => cache.TryGetValue(Key(userId), out newSecurityStamp!);
}
