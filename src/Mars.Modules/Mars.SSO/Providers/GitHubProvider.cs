using System.CommandLine.Parsing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;

namespace Mars.SSO.Providers;

// GitHub: note GitHub does not use OIDC for all setups; can be OAuth2 + user API
public class GitHubProvider : ISsoProvider
{
    private readonly SsoProviderDescriptor _desc;
    private readonly IHttpClientFactory _httpFactory;

    public GitHubProvider(SsoProviderDescriptor desc, IHttpClientFactory httpFactory)
    {
        _desc = desc;
        _httpFactory = httpFactory;
    }

    public string Name => _desc.Name;
    public string DisplayName => _desc.DisplayName ?? _desc.Name;
    public string? IconUrl => _desc.IconUrl;

    public string GetAuthorizationUrl(string state, string redirectUri, string? scope = null)
    {
        var scopeToUse = scope ?? "read:user user:email";
        var authorize = _desc.AuthorizationEndpoint ?? "https://github.com/login/oauth/authorize";
        return authorize +
        $"?client_id={Uri.EscapeDataString(_desc.ClientId ?? "")}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scopeToUse)}&state={Uri.EscapeDataString(state)}";
    }

    public async Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri)
    {
        var tokenEndpoint = _desc.TokenEndpoint ?? "https://github.com/login/oauth/access_token";
        var client = _httpFactory.CreateClient();
        var pairs = new Dictionary<string, string>
        {
            ["client_id"] = _desc.ClientId!,
            ["client_secret"] = _desc.ClientSecret ?? "",
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        };

        /*
        var res = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(pairs));
        var txt = await res.Content.ReadAsStringAsync();
        // GitHub returns urlencoded body by default; extract access_token
        var values = System.Web.HttpUtility.ParseQueryString(txt);
        var accessToken = values["access_token"] ?? (JsonDocument.Parse(txt).RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null);
        if (accessToken == null) return null;
        */
        var token = await ExchangeCodeForTokenAsync(tokenEndpoint, pairs);
        if (token == null) return null;

        var _OAuth = JsonSerializer.Deserialize<OAuthTokenResponse>(token.Value);

        // fetch user
        if (false)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _OAuth.AccessToken);
            var userResp = await client.GetAsync(_desc.UserInfoEndpoint ?? "https://api.github.com/user");
            if (!userResp.IsSuccessStatusCode) return null;
            var userJson = await userResp.Content.ReadFromJsonAsync<JsonElement>();
            var id = userJson.GetProperty("id").GetRawText();
            var login = userJson.TryGetProperty("login", out var loginP) ? loginP.GetString() : null;
            // get email (may need separate call)
            string? email = null;
            try
            {
                if (userJson.TryGetProperty("email", out var emailP) && emailP.ValueKind == JsonValueKind.String)
                    email = emailP.GetString();
            }
            catch { }

            var userInfo = new SsoUserInfo
            {
                InternalId = Guid.Empty,
                ExternalId = id,
                Email = email ?? string.Empty,
                Name = login,
                Provider = Name,
                //RawData = new Dictionary<string, object> { ["user"] = userJson },
                AccessToken = _OAuth.AccessToken,
                UserPrimaryInfo = null!,
            };
        }

        return new()
        {
            AccessToken = _OAuth.AccessToken,
            OAuthResponse = _OAuth,
            RawResponse = token.Value
        };

    }

    private async Task<JsonElement?> ExchangeCodeForTokenAsync(string tokenEndpoint, Dictionary<string, string> form)
    {
        var client = _httpFactory.CreateClient();
        var res = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form));
        if (!res.IsSuccessStatusCode) return null;
        var json = await res.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        // GitHub uses opaque tokens for OAuth2 by default — no JWT validation
        return Task.FromResult<ClaimsPrincipal?>(null);
    }

    public UpsertUserRemoteDataQuery MapToCreateUserQuery(ClaimsPrincipal principal)
    {
        throw new NotImplementedException();
    }
}
