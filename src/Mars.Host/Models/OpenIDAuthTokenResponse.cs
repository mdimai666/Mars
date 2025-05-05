using System.Text.Json.Serialization;

namespace Mars.Host.Models;

public class OpenIDAuthTokenResponse
{

    [JsonPropertyName("access_token"), Newtonsoft.Json.JsonProperty("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expires_in"), Newtonsoft.Json.JsonProperty("expires_in")]
    public required long ExpiresIn { get; init; }

    [JsonPropertyName("refresh_expires_in"), Newtonsoft.Json.JsonProperty("refresh_expires_in")]
    public required long RefreshExpiresIn { get; init; }

    [JsonPropertyName("refresh_token"), Newtonsoft.Json.JsonProperty("refresh_token")]
    public required string? RefreshToken { get; init; }

    [JsonPropertyName("token_type"), Newtonsoft.Json.JsonProperty("token_type")]
    public required string TokenType { get; init; }

    [JsonPropertyName("session_state"), Newtonsoft.Json.JsonProperty("session_state")]
    public required string? SessionState { get; init; }

    [JsonPropertyName("scope"), Newtonsoft.Json.JsonProperty("scope")]
    public required string Scope { get; init; }

}

