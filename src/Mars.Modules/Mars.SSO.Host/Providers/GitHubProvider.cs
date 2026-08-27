using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Web;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;
using Mars.Shared.Contracts.Users;
using Mars.SSO.Dto;
using Mars.SSO.Mappings;
using Microsoft.Extensions.Logging;

namespace Mars.SSO.Providers;

// GitHub: note GitHub does not use OIDC for all setups; can be OAuth2 + user API
public class GitHubProvider : ISsoProvider
{
    private readonly SsoProviderDescriptor _desc;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<GitHubProvider> _logger;

    public GitHubProvider(SsoProviderDescriptor desc, IHttpClientFactory httpFactory, ILogger<GitHubProvider> logger)
    {
        _desc = desc;
        _httpFactory = httpFactory;
        _logger = logger;
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
        if (!res.IsSuccessStatusCode)
        {
            var errDetail = await res.Content.ReadAsStringAsync();
            _logger.LogError($"ExchangeCodeForTokenAsync: {res.StatusCode} " + errDetail);
            return null;
        }
        var json = ConvertFormUrlEncodedToJsonElement(await res.Content.ReadAsStringAsync());
        return json;
    }

    static JsonElement ConvertFormUrlEncodedToJsonElement(string formUrlEncoded)
    {
        // Парсим FormUrlEncoded строку
        var nameValueCollection = HttpUtility.ParseQueryString(formUrlEncoded);

        var dictionary = new Dictionary<string, string>();
        foreach (string key in nameValueCollection.AllKeys!)
        {
            if (key != null)
            {
                dictionary[key] = nameValueCollection[key]!;
            }
        }

        string jsonString = JsonSerializer.Serialize(dictionary);
        JsonDocument doc = JsonDocument.Parse(jsonString);

        return doc.RootElement.Clone(); // Clone для безопасного использования после Dispose
    }

    private List<Claim> GetClaims(GithubUserInfoResponse user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.id.ToString()),
            new(ClaimTypes.Name, user.login),
            new(ClaimTypes.Email, user.email ?? ""),
            new(ClaimTypes.GivenName, user.name??user.login??""),
            new(ClaimTypes.Surname, ""),
            new(ClaimTypes.Role, "Viewer"),
        };

        if (!string.IsNullOrEmpty(user.avatar_url))
            claims.Add(new("picture", user.avatar_url));

        return claims;
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        // GitHub uses opaque tokens for OAuth2 by default — no JWT validation
        //return Task.FromResult<ClaimsPrincipal?>(null);
        var client = _httpFactory.CreateClient();

        // fetch user
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MarsApp/1.0");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var userResp = await client.GetAsync(_desc.UserInfoEndpoint ?? "https://api.github.com/user");
        if (!userResp.IsSuccessStatusCode) return null;
        var userInfo = await userResp.Content.ReadFromJsonAsync<GithubUserInfoResponse>();
        //var id = userInfo.id;
        //var login = userInfo.login;
        //// get email (may need separate call)
        //string? email = userInfo.email;

        var claims = GetClaims(userInfo!);
        var identity = new ClaimsIdentity(claims);
        var principial = new ClaimsPrincipal(identity);

        return principial;
    }

    public UpsertUserRemoteDataQuery MapToCreateUserQuery(ClaimsPrincipal principal)
    {
        var claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value);
        var externalKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string[] roles = principal.FindFirstValue(ClaimTypes.Role) != null ? [principal.FindFirstValue(ClaimTypes.Role)!] : GenericOidcProvider.DefaultExternalUserRoles;

        return new()
        {
            ExternalKey = externalKey ?? throw new InvalidOperationException("Missing 'sub' claim in Google token"),
            PreferredUserName = claims.GetValueOrDefault(ClaimTypes.Name) ?? claims.GetValueOrDefault(ClaimTypes.Email)?.Split('@', 2)[0] ?? Guid.NewGuid().ToString("N"),
            FirstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown",
            LastName = principal.FindFirstValue(ClaimTypes.Surname) ?? "",
            MiddleName = null,
            Email = principal.FindFirstValue(ClaimTypes.Email),
            Roles = roles,
            Gender = UserGender.None,
            AvatarUrl = claims.GetValueOrDefault("picture"),
            BirthDate = null,
            PhoneNumber = null,
            Prodvider = _desc.ToInfo()
        };
    }
}
