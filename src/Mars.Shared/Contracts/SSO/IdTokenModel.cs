using System.Text.Json.Serialization;

namespace Mars.Shared.Contracts.SSO;

public class IdTokenModel
{
    // Unique user identifier (������ ����������)
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = default!;

    // ��������� (���� ������������ ����� � ������ client_id)
    [JsonPropertyName("aud")]
    public string? Audience { get; set; }

    // Issuer (URL realm'� ��� Google accounts)
    [JsonPropertyName("iss")]
    public string? Issuer { get; set; }

    // ����� ������� ������ (Unix time)
    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    // ����� ��������� ������ (Unix time)
    [JsonPropertyName("exp")]
    public long ExpiresAt { get; set; }

    // Email ������������
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    // ���������� �� email
    [JsonPropertyName("email_verified")]
    public bool? EmailVerified { get; set; }

    // ���
    [JsonPropertyName("given_name")]
    public string? FirstName { get; set; }

    // �������
    [JsonPropertyName("family_name")]
    public string? LastName { get; set; }

    // ������ ���
    [JsonPropertyName("name")]
    public string? FullName { get; set; }

    // URL �������
    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    // ������ (��������, "ru-RU")
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    // Nonce, ���� ������������� ��� ������� (��� ������ �� replay)
    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }

    // Session ID (� Keycloak ����)
    [JsonPropertyName("session_state")]
    public string? SessionState { get; set; }
}
