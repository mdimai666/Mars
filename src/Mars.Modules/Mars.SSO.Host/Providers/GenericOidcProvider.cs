using System.Security.Claims;
using System.Text.Json;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;
using Mars.SSO.Utilities;
using Microsoft.Extensions.Logging;

namespace Mars.SSO.Providers;

public abstract class GenericOidcProvider : ISsoProvider
{
    public static readonly string[] DefaultExternalUserRoles = ["Viewer"];

    protected readonly SsoProviderDescriptor _descriptor;
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly OidcMetadataCache _metadataCache;
    protected readonly ILogger _logger;

    public GenericOidcProvider(SsoProviderDescriptor descriptor, IHttpClientFactory httpClientFactory, OidcMetadataCache metadataCache, ILogger logger)
    {
        _descriptor = descriptor;
        _httpClientFactory = httpClientFactory;
        _metadataCache = metadataCache;
        _logger = logger;
    }

    public string Name => _descriptor.Name;
    public string DisplayName => _descriptor.DisplayName ?? _descriptor.Name;
    public string? IconUrl => _descriptor.IconUrl;

    public virtual string GetAuthorizationUrl(string state, string redirectUri, string? scope = null)
    {
        var scopeToUse = scope ?? "openid profile email";
        if (!string.IsNullOrEmpty(_descriptor.AuthorizationEndpoint))
        {
            return _descriptor.AuthorizationEndpoint +
            $"?client_id={Uri.EscapeDataString(_descriptor.ClientId ?? "")}&response_type=code&scope={Uri.EscapeDataString(scopeToUse)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}";
        }

        // fallback to discovery
        if (!string.IsNullOrEmpty(_descriptor.Issuer))
        {
            var auth = (_metadataCache.GetConfigurationAsync(_descriptor.Issuer).GetAwaiter().GetResult())?.AuthorizationEndpoint;
            return auth +
            $"?client_id={Uri.EscapeDataString(_descriptor.ClientId ?? "")}&response_type=code&scope={Uri.EscapeDataString(scopeToUse)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}";
        }

        throw new InvalidOperationException("No authorization endpoint configured for provider: " + Name);
    }

    public abstract Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri);

    public abstract Task<ClaimsPrincipal?> ValidateTokenAsync(string token);

    protected async Task<JsonElement?> ExchangeCodeForTokenAsync(string tokenEndpoint, Dictionary<string, string> form)
    {
        var client = _httpClientFactory.CreateClient();
        var res = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form));
        if (!res.IsSuccessStatusCode)
        {
            var errDetail = await res.Content.ReadAsStringAsync();
            _logger.LogError($"ExchangeCodeForTokenAsync: {res.StatusCode} " + errDetail);
            return null;
        }
        var json = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    public abstract UpsertUserRemoteDataQuery MapToCreateUserQuery(ClaimsPrincipal principal);
}
