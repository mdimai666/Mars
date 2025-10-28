using System.Text.Json.Serialization;

namespace Mars.Host.Models;

[Obsolete]
public class OpenIDAuthTokenResponse
{

    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public required long ExpiresIn { get; init; }

    [JsonPropertyName("refresh_expires_in")]
    public required long RefreshExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")]
    public required string? RefreshToken { get; init; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    [JsonPropertyName("session_state")]
    public required string? SessionState { get; init; }

    [JsonPropertyName("scope")]
    public required string Scope { get; init; }

}
