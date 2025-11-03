using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mars.Host.Shared.SSO.Dto;

public record SsoTokenResponse
{
    public required string AccessToken { get; init; }
    public required OAuthTokenResponse OAuthResponse { get; init; }
    public required JsonElement RawResponse { get; init; }

}

public class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    [JsonPropertyName("refresh_token")]
    public required string? RefreshToken { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("expires_in")]
    public long? ExpiresIn { get; init; }

    [JsonPropertyName("refresh_expires_in")]
    public long? RefreshExpiresIn { get; init; }

    [JsonPropertyName("session_state")]
    public string? SessionState { get; init; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }
}
