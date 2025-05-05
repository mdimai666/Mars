using System.Text.Json.Serialization;

namespace Mars.Host.Services.Keycloak;

public class KeycloakUserInfo
{
    [JsonPropertyName("sub"), Newtonsoft.Json.JsonProperty("sub")]
    public Guid Id { get; init; }

    [JsonPropertyName("name"), Newtonsoft.Json.JsonProperty("name")]
    public string Fullname { get; init; }

    [JsonPropertyName("given_name"), Newtonsoft.Json.JsonProperty("given_name")]
    public string Firstname { get; init; }

    [JsonPropertyName("family_name"), Newtonsoft.Json.JsonProperty("family_name")]
    public string Lastname { get; init; }

    [JsonPropertyName("email"), Newtonsoft.Json.JsonProperty("email")]
    public string Email { get; init; }

    [JsonPropertyName("email_verified"), Newtonsoft.Json.JsonProperty("email_verified")]
    public bool EmailVerified { get; init; }

    [JsonPropertyName("preferred_username"), Newtonsoft.Json.JsonProperty("preferred_username")]
    public string PreferredUsername { get; init; }

    [JsonPropertyName("scope"), Newtonsoft.Json.JsonProperty("scope")]
    public string Scope { get; init; }
    //sub

    [JsonPropertyName("exp"), Newtonsoft.Json.JsonProperty("exp")]
    public long Expire { get; set; }

    [JsonPropertyName("iss"), Newtonsoft.Json.JsonProperty("iss")]
    public string Issuer { get; init; }

    [JsonPropertyName("aud"), Newtonsoft.Json.JsonProperty("aud")]
    public string aud { get; init; }

    [JsonPropertyName("allowed-origins"), Newtonsoft.Json.JsonProperty("allowed-origins")]
    public string[] AllowedOrigins { get; init; }

    [JsonPropertyName("realm_access"), Newtonsoft.Json.JsonProperty("realm_access")]
    public KeycloakTokenRealmAccess RealmAccess { get; init; } = new();

    public class KeycloakTokenRealmAccess
    {
        [JsonPropertyName("roles"), Newtonsoft.Json.JsonProperty("roles")]
        public string[] Roles { get; init; } = [];
    }
}
