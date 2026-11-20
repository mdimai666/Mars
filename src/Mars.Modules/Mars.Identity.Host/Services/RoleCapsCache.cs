using System.Collections.Frozen;
using Mars.Identity.Abstractions.Dto.Roles;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.ViewModels;
using Mars.Server.Abstractions.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Identity.Host.Services;

/// <summary>
/// Паттерн снимка — как в <see cref="Locators.UserMetaLocator"/>: volatile + SemaphoreSlim,
/// scoped-репозиторий захватывается через IServiceScopeFactory на время загрузки.
/// </summary>
internal sealed class RoleCapsCache : IRoleCapsCache
{
    private volatile FrozenDictionary<string, IReadOnlyCollection<string>>? _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;

    public RoleCapsCache(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetRoleCaps(CancellationToken cancellationToken = default)
    {
        if (_cache is { } snapshot)
            return snapshot;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is { } cached)
                return cached;

            var loaded = await _scopeFactory.ExecuteScopedAsync<IRoleRepository, FrozenDictionary<string, IReadOnlyCollection<string>>>(async repo =>
            {
                var roles = await repo.ListAll(cancellationToken);
                var claims = await repo.ListAllClaims(cancellationToken);

                var capsByRoleId = claims
                    .Where(s => s.ClaimType == RoleCaps.ClaimType)
                    .GroupBy(s => s.RoleId)
                    .ToDictionary(g => g.Key, g => g.Select(s => s.ClaimValue).ToFrozenSet());

                return roles.ToFrozenDictionary<RoleSummary, string, IReadOnlyCollection<string>>(
                    r => r.Name,
                    r => capsByRoleId.TryGetValue(r.Id, out var caps) ? caps : FrozenSet<string>.Empty,
                    StringComparer.OrdinalIgnoreCase);
            });

            _cache = loaded;
            return loaded;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate()
        => _cache = null;
}
