using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Mars.SSO.Utilities;

public class OidcMetadataCache
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpFactory;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(6);

    public OidcMetadataCache(IMemoryCache cache, IHttpClientFactory httpFactory)
    {
        _cache = cache;
        _httpFactory = httpFactory;
    }

    public async Task<OpenIdConnectConfiguration?> GetConfigurationAsync(string authority, string? discoveryEndpoint = null)
    {
        var key = $"oidc:config:{authority}";
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _defaultTtl;
            var http = _httpFactory.CreateClient();
            var discoUrl = discoveryEndpoint ?? (authority.TrimEnd('/') + "/.well-known/openid-configuration");

            var documentRetriever = new HttpDocumentRetriever(http)
            {
                RequireHttps = false // ✅ Разрешаем HTTP для localhost
            };

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(discoUrl, new OpenIdConnectConfigurationRetriever(), documentRetriever);
            try
            {
                return await configManager.GetConfigurationAsync(default);
            }
            catch
            {
                // swallow and return null to indicate failure
                return null;
            }
        });
    }
}
