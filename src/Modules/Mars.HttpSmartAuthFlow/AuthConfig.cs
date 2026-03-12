using Mars.HttpSmartAuthFlow.Strategies;

namespace Mars.HttpSmartAuthFlow;

public class AuthConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public AuthMode Mode { get; set; }
    public string? CustomType { get; set; }

    // Common
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;

    // Bearer Token
    public string? TokenUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Scope { get; set; }

    // Cookie Form
    public string? LoginPageUrl { get; set; }
    public bool FollowRedirects { get; set; } = true;

    // Cookie Endpoint (через прямой API вызов)
    public CookieEndpointConfig? CookieEndpointConfig { get; set; }

    // API Key
    public string? ApiKey { get; set; }
    public string? ApiKeyHeaderName { get; set; } = "X-API-Key";
}
