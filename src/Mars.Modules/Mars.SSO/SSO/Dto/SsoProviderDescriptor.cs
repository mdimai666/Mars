namespace Mars.Host.Shared.SSO.Dto;

public class SsoProviderDescriptor
{
    public string Name { get; set; } = ""; // unique key like "keycloak", "google"
    public string Driver { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Issuer { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? UserInfoEndpoint { get; set; }
    public string? JwksUri { get; set; }
    public string? RedirectUri { get; set; }
    public bool IsOidc { get; set; } = true; // OIDC (discovery) vs custom OAuth2
    public string? Algorithm { get; set; } // e.g. RS256, HS256
    public string? SigningKey { get; set; } // for HS256 or PEM for RS256 (optional)
    public bool IsEnabled { get; set; } = true;
    public string? IconUrl { get; set; }
}
