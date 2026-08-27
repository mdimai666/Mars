using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.SSO.Dto;
using Mars.Options.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.SSO.Services;

public interface ISsoProviderRepository
{
    Task<IEnumerable<SsoProviderDescriptor>> ListEnabledAsync();
    Task<SsoProviderDescriptor?> GetByNameAsync(string name);
    bool TryValidateIssuer(string issuer, out SsoProviderDescriptor? ssoProviderDescriptor);
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
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(30);

    public SsoOptionsProviderRepository(IOptionService optionService, IMemoryCache cache)
    {
        _optionService = optionService;
        _cache = cache;
    }

    private IEnumerable<SsoProviderDescriptor> Providers
    {
        get
        {
            var dynamic = _cache.GetOrCreate("sso:providers:descriptors", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
                var descriptors = ListEnabledAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return descriptors.ToList();
            }) as List<SsoProviderDescriptor> ?? [];

            return dynamic;
        }
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
            IconUrl = config.IconUrl.AsNullIfEmpty(),
        };

    public bool TryValidateIssuer(string issuer, out SsoProviderDescriptor? ssoProviderDescriptor)
    {
        var found = Providers.FirstOrDefault(s => s.Issuer == issuer);
        ssoProviderDescriptor = found;
        return found is not null;
    }
}
