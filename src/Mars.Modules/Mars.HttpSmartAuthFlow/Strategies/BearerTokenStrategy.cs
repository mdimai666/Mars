using Mars.HttpSmartAuthFlow.Dto;
using Mars.HttpSmartAuthFlow.Exceptions;

namespace Mars.HttpSmartAuthFlow.Strategies;

public class BearerTokenStrategy : AuthStrategyBase
{
    private string? _accessToken;
    private DateTime _tokenExpiry;
    private readonly HttpClient _httpClient;

    public BearerTokenStrategy(AuthConfig config) : base(config)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds)
        };
    }

    public override async Task ApplyAuthenticationAsync(HttpRequestMessage request)
    {
        var token = await GetTokenAsync();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public override async Task<bool> HandleUnauthorizedAsync(HttpRequestMessage request)
    {
        await InvalidateAsync();
        await ApplyAuthenticationAsync(request);
        return true;
    }

    public override async Task InvalidateAsync()
    {
        await ExecuteWithLockAsync(() =>
        {
            _accessToken = null;
            _isAuthenticated = false;
            return Task.CompletedTask;
        });
    }

    private async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(2))
        {
            return _accessToken;
        }

        await ExecuteWithLockAsync(async () =>
        {
            // Проверяем еще раз после получения лока
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(2))
            {
                return;
            }

            await RefreshTokenAsync();
        });

        return _accessToken!;
    }

    private async Task RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(Config.TokenUrl))
            throw new InvalidOperationException("TokenUrl is required for BearerToken mode");

        var formData = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "password"),
            new("username", Config.Username!),
            new("password", Config.Password!)
        };

        if (!string.IsNullOrEmpty(Config.ClientId))
            formData.Add(new("client_id", Config.ClientId));

        if (!string.IsNullOrEmpty(Config.ClientSecret))
            formData.Add(new("client_secret", Config.ClientSecret));

        if (!string.IsNullOrEmpty(Config.Scope))
            formData.Add(new("scope", Config.Scope));

        try
        {
            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(Config.TokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new AuthenticationException($"Token request failed: {response.StatusCode}. {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(json)
                ?? throw new AuthenticationException("Invalid token response");

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            _isAuthenticated = true;
            _lastAuthTime = DateTime.UtcNow;

            Console.WriteLine($"[{Config.Id}] Bearer token refreshed. Expires in {tokenResponse.ExpiresIn}s");
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("Failed to obtain access token", ex);
        }
    }

    public override void Dispose()
    {
        _httpClient.Dispose();
        _authLock.Dispose();
    }
}
