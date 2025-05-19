using System.Text.Json.Serialization;

namespace Mars.Host.Services.Keycloak;

public class KeycloakUserInfo
{
    [JsonPropertyName("sub")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Fullname { get; init; }

    [JsonPropertyName("given_name")]
    public string Firstname { get; init; }

    [JsonPropertyName("family_name")]
    public string Lastname { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; init; }

    [JsonPropertyName("preferred_username")]
    public string PreferredUsername { get; init; }

    [JsonPropertyName("scope")]
    public string Scope { get; init; }
    //sub

    [JsonPropertyName("exp")]
    public long Expire { get; set; }

    [JsonPropertyName("iss")]
    public string Issuer { get; init; }

    [JsonPropertyName("aud")]
    public string aud { get; init; }

    [JsonPropertyName("allowed-origins")]
    public string[] AllowedOrigins { get; init; }

    [JsonPropertyName("realm_access")]
    public KeycloakTokenRealmAccess RealmAccess { get; init; } = new();

    public class KeycloakTokenRealmAccess
    {
        [JsonPropertyName("roles")]
        public string[] Roles { get; init; } = [];
    }
}
