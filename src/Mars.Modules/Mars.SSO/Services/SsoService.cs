using System.Security.Claims;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;
using Mars.Host.Shared.SSO.Services;
using Mars.SSO.Providers;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.SSO.Services;

internal class SsoService : ISsoService
{
    private readonly ISsoProviderRepository _repo;
    private readonly DynamicSsoProviderFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly IExperimentalSignInService _experimentalSignInService;
    private readonly IUserService _userService;
    private readonly IEnumerable<ISsoProvider> _staticProviders; // e.g., LocalJwtProvider if registered
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(30);

    public SsoService(ISsoProviderRepository repo,
                        DynamicSsoProviderFactory factory,
                        IMemoryCache cache,
                        IExperimentalSignInService experimentalSignInService,
                        IUserService userService,
                        IEnumerable<ISsoProvider> staticProviders)
    {
        _repo = repo;
        _factory = factory;
        _cache = cache;
        _experimentalSignInService = experimentalSignInService;
        _userService = userService;
        _staticProviders = staticProviders;
    }

    public IEnumerable<SsoProviderDescriptor> Providers
    {
        get
        {
            var dynamic = _cache.GetOrCreate("sso:providers:descriptors", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
                var descriptors = _repo.ListEnabledAsync().GetAwaiter().GetResult();
                return descriptors.ToList();
            }) as List<SsoProviderDescriptor> ?? [];

            return dynamic;
        }
    }

    public IEnumerable<ISsoProvider> CreateProviderList()
    {
        return Providers.Select(d => _factory.Create(d));
    }

    public ISsoProvider? GetProvider(string name)
    {
        var d = Providers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        var provider = _factory.Create(d);
        return provider;
    }

    public async Task<SsoUserInfo?> AuthenticateAsync(string providerName, string code, string redirectUri)
    {
        var provider = GetProvider(providerName);
        if (provider == null) throw new InvalidOperationException("Unknown provider");

        var authResult = await provider.AuthenticateAsync(code, redirectUri);

        if (authResult is null) return null;

        //var claims = ExtractClaims(handler, accessToken) ?? throw new UserActionException("ExtractClaims error");
        var claims = await provider.ValidateTokenAsync(authResult.OAuthResponse.IdToken ?? authResult.AccessToken) ?? throw new ArgumentNullException();

        //var handler = new JwtSecurityTokenHandler();
        //var jwt = handler.ReadJwtToken(authResult.OAuthResponse.IdToken ?? authResult.AccessToken);
        //var claims = jwt.Claims;

        var userData = provider.MapToCreateUserQuery(claims);
        var internalUser = await _userService.RemoteUserUpsert(userData, CancellationToken.None);

        await _experimentalSignInService.LoginForceByNameIdentifierAsync(providerName, userData.ExternalKey, CancellationToken.None);

        return new SsoUserInfo
        {
            ExternalId = userData.ExternalKey,
            InternalId = internalUser.Id,
            Name = internalUser.UserName,
            Email = userData.Email,
            Provider = providerName,
            AccessToken = authResult.OAuthResponse.IdToken ?? authResult.AccessToken,
            UserPrimaryInfo = new Shared.ViewModels.UserPrimaryInfo
            {
                Id = internalUser.Id,
                Username = internalUser.UserName,
                Email = internalUser.Email,
                FirstName = internalUser.FirstName,
                LastName = internalUser.LastName,
                Roles = internalUser.Roles,
                AvatarUrl = userData.AvatarUrl,
            }
        };
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        // try static providers first
        foreach (var p in _staticProviders)
        {
            var pr = await p.ValidateTokenAsync(token);
            if (pr != null) return pr;
        }

        // then dynamic
        var descriptors = await _repo.ListEnabledAsync();
        foreach (var d in descriptors)
        {
            var provider = _factory.Create(d);
            var pr = await provider.ValidateTokenAsync(token);
            if (pr != null) return pr;
        }

        return null;
    }

    public bool TryValidateIssuer(string issuer, out SsoProviderDescriptor? ssoProviderDescriptor)
    {
        var found = Providers.FirstOrDefault(s => s.Issuer == issuer);
        ssoProviderDescriptor = found;
        return found is not null;
    }
}
