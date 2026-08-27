using System.Text.Json.Serialization;

namespace Mars.Host.Shared.SSO.Dto;

public record OpenIdTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; } = "bearer";

    [JsonPropertyName("expires_in")]
    public required long ExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")]
    public required string? RefreshToken { get; init; }
}
