using System.Text.Json.Serialization;

namespace Mars.Shared.Contracts.SSO;

public class IdTokenModel
{
    // Unique user identifier (всегда обязателен)
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = default!;

    // Аудитория (кому предназначен токен — обычно client_id)
    [JsonPropertyName("aud")]
    public string? Audience { get; set; }

    // Issuer (URL realm'а или Google accounts)
    [JsonPropertyName("iss")]
    public string? Issuer { get; set; }

    // Время выпуска токена (Unix time)
    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    // Время истечения токена (Unix time)
    [JsonPropertyName("exp")]
    public long ExpiresAt { get; set; }

    // Email пользователя
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    // Подтверждён ли email
    [JsonPropertyName("email_verified")]
    public bool? EmailVerified { get; set; }

    // Имя
    [JsonPropertyName("given_name")]
    public string? FirstName { get; set; }

    // Фамилия
    [JsonPropertyName("family_name")]
    public string? LastName { get; set; }

    // Полное имя
    [JsonPropertyName("name")]
    public string? FullName { get; set; }

    // URL аватара
    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    // Локаль (например, "ru-RU")
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    // Nonce, если использовался при запросе (для защиты от replay)
    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }

    // Session ID (в Keycloak есть)
    [JsonPropertyName("session_state")]
    public string? SessionState { get; set; }
}
