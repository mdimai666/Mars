using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Mars.SSO.Host.OAuth.interfaces;
using Mars.SSO.Host.OAuth.Models;

namespace Mars.SSO.Host.OAuth.Services;

public class InMemoryClientStore : IOAuthClientStore
{
    private Dictionary<string, OAuthClient> _clients = default!;
    private readonly IOptionService _optionService;
    private readonly ITokenService _tokenService;
    private OpenIDServerOption _openIDServerOption = default!;

    public InMemoryClientStore(IOptionService optionService, ITokenService tokenService)
    {
        _optionService = optionService;
        _tokenService = tokenService;
        RefreshOptions();

        optionService.OnOptionUpdate += OptionService_OnOptionUpdate;
    }

    private void OptionService_OnOptionUpdate(object obj)
    {
        if (obj is OpenIDServerOption)
            RefreshOptions();
    }

    public OAuthClient? FindClientById(string clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    void RefreshOptions()
    {
        _openIDServerOption = _optionService.GetOption<OpenIDServerOption>();
        _clients = _openIDServerOption.OpenIDClientConfigs
            .Where(c => c.Enable)
            .ToDictionary(c => c.ClientId, Map);
    }

    private OAuthClient Map(OpenIDServerClientConfig opt)
        => new()
        {
            ClientId = opt.ClientId,
            ClientSecret = opt.ClientSecret,
            RedirectUris = opt.RedirectUris,
            AllowedGrantTypes = opt.AllowedGrantTypes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            RequirePkce = opt.RequirePkce,
            AccessTokenLifetimeSeconds = _tokenService.ExpiryInSeconds,
            RefreshTokenLifetimeDays = 60, //TODO: make configurable
            AllowOfflineAccess = true,
            AllowedScopes = "openid profile email phone address roles api1"
        };
}
