using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.SSO.Dto;
using Mars.Options.Models;

namespace Mars.SSO.Services;

public interface ISsoProviderRepository
{
    Task<IEnumerable<SsoProviderDescriptor>> ListEnabledAsync();
    Task<SsoProviderDescriptor?> GetByNameAsync(string name);
}
/*
public class EfProviderRepository : IProviderRepository
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

    public EfProviderRepository(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IEnumerable<SsoProviderDescriptor>> ListEnabledAsync()
    {
        return await _cache.GetOrCreateAsync("sso:providers:enabled", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
            return await _db.SsoProviders.Where(p => p.IsEnabled).ToListAsync();
        });
    }

    public async Task<SsoProviderDescriptor?> GetByNameAsync(string name)
    {
        var key = $"sso:provider:{name}";
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
            return await _db.SsoProviders.FirstOrDefaultAsync(p => p.Name == name && p.IsEnabled);
        });
    }
}
*/

public class SsoOptionsProviderRepository : ISsoProviderRepository
{
    private readonly IOptionService _optionService;

    public SsoOptionsProviderRepository(IOptionService optionService)
    {
        _optionService = optionService;
    }

    public Task<IEnumerable<SsoProviderDescriptor>> ListEnabledAsync()
    {
        return Task.FromResult(_optionService.GetOption<OpenIDClientOption>()
                            .OpenIDClientConfigs
                            .Where(s => s.Enable)
                            .Select(ToDto));
    }

    public Task<SsoProviderDescriptor?> GetByNameAsync(string name)
    {
        var option = _optionService.GetOption<OpenIDClientOption>()
                            .OpenIDClientConfigs
                            .FirstOrDefault(s => s.Enable && s.Slug == name);
        return Task.FromResult(option == null ? null : ToDto(option));
    }

    SsoProviderDescriptor ToDto(OpenIDClientConfig config)
        => new()
        {
            Name = config.Slug,
            Driver = config.Driver,
            DisplayName = config.Title,
            ClientId = config.ClientId,
            ClientSecret = config.ClientSecret,
            AuthorizationEndpoint = config.AuthEndpoint,
            TokenEndpoint = config.TokenEndpoint,
            IsEnabled = config.Enable,
            Issuer = config.Issuer,
            IconUrl = config.IconUrl.AsNullIfEmpty()
        };
}
