using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Mars.Shared.Contracts.SSO;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace AppFront.Shared.AuthProviders;

public class CookieOrLocalStorageAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _js;
    private readonly ILogger<CookieOrLocalStorageAuthStateProvider> _logger;
    private readonly string _tokenKey = "authToken";
    private readonly string _refreshUrl = "auth/refresh"; // 👈 твой эндпоинт для обновления токена
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CookieOrLocalStorageAuthStateProvider(HttpClient httpClient, IJSRuntime js, ILogger<CookieOrLocalStorageAuthStateProvider> logger)
    {
        _httpClient = httpClient;
        _js = js;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        //Console.WriteLine(">GetAuthenticationStateAsync:1");
        var token = await TryGetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
            return new AuthenticationState(_anonymous);

        if (Q.Site.InitailUserPrimaryInfo is null)
            return new AuthenticationState(_anonymous);

        // Настроим HTTP заголовок
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Проверим срок действия
        var jwtClaims = ParseClaimsFromJwt(token).ToList();
        var exp = jwtClaims.FirstOrDefault(c => c.Type == "exp")?.Value;

        //Console.WriteLine(">GetAuthenticationStateAsync:2");
        if (exp != null && DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)) <= DateTimeOffset.UtcNow)
        {
            //    // 🧠 Протух — пробуем рефреш из куки
            //    token = await TryRefreshAccessTokenAsync();
            //    if (token == null)
            return new AuthenticationState(_anonymous);

            //    await _js.InvokeVoidAsync("localStorage.setItem", _tokenKey, token);
            //    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            //    jwtClaims = ParseClaimsFromJwt(token).ToList();
        }

        //Console.WriteLine(">GetAuthenticationStateAsync:3");
        UpdateQUserDataFromJwt();
        var identity = new ClaimsIdentity(jwtClaims, "jwt");
        var user = new ClaimsPrincipal(identity);
        EnrichUserClaims(user);
        //Console.WriteLine("GetAuthenticationStateAsync");

        return new AuthenticationState(user);
    }

    public async Task MarkUserAsAuthenticated(string token, SsoUserInfoResponse? ssoUserInfo = null)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", _tokenKey, token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        if (ssoUserInfo == null)
        {
            Q.UpdateUserByMarsClaims(user);
        }
        else
        {
            EnrichUserClaims(user);
            Q.UpdateUserByExternalClaims(user, ssoUserInfo);
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", _tokenKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
        Q.LogoutUser();
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    private async Task<string?> TryGetAccessTokenAsync()
    {
        // 1️⃣ localStorage
        var token = await _js.InvokeAsync<string?>("localStorage.getItem", _tokenKey);
        if (!string.IsNullOrEmpty(token))
            return token;

        // 2️⃣ кука
        return await TryGetTokenFromCookieAsync("authToken");
    }

    private async Task<string?> TryRefreshAccessTokenAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(_refreshUrl, null);
            if (response.StatusCode != HttpStatusCode.OK)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("access_token", out var tokenProp))
                return tokenProp.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        try
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var json = JsonSerializer.Deserialize<JsonElement>(jsonBytes);

            var claims = new List<Claim>();

            foreach (var property in json.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(property.Name, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(property.Name, property.Value.ToString()));
                }
            }

            return claims;
        }
        catch (Exception ex)
        {
            //Console.Error.WriteLine("ERROR ParseClaimsFromJwt: " + ex.Message);
            _logger.LogError(ex, "ERROR ParseClaimsFromJwt");
        }
        return [];
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }

    private async Task<string?> TryGetTokenFromCookieAsync(string cookieName)
    {
        try
        {
            return await _js.InvokeAsync<string>(
                "(function(name){ const m = document.cookie.match(new RegExp(name + '=([^;]+)')); return m ? m[1] : null; })",
                cookieName);
        }
        catch
        {
            return null;
        }
    }

    private void EnrichUserClaims(ClaimsPrincipal principal)
    {
        if (Q.Site.InitailUserPrimaryInfo is null) return;

        var marsIdentity = new ClaimsIdentity([
            new (ClaimTypes.NameIdentifier, Q.Site.InitailUserPrimaryInfo.Id.ToString()),
            new (ClaimTypes.Email, Q.Site.InitailUserPrimaryInfo.Email??""),
            new (ClaimTypes.GivenName, Q.Site.InitailUserPrimaryInfo.FirstName),
            new (ClaimTypes.Surname, Q.Site.InitailUserPrimaryInfo.LastName),
        ]);
        foreach (var role in Q.Site.InitailUserPrimaryInfo.Roles)
        {
            marsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
        principal.AddIdentity(marsIdentity);
    }

    private void UpdateQUserDataFromJwt()
    {
        if (Q.Site.InitailUserPrimaryInfo != null && Q.User.Id == Guid.Empty)
            Q.UpdateUserByInitailVM(Q.Site.InitailUserPrimaryInfo, null);
    }
}
