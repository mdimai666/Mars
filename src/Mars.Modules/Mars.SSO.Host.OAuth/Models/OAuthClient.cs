using Mars.Identity.Abstractions.Utils;

namespace Mars.SSO.Host.OAuth.Models;

// OAuthClient — зарегистрированные клиенты
public class OAuthClient
{
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// SHA-256 хэш секрета (base64, контракт <see cref="ApiKeyFormat.HashSecret"/>).
    /// Plaintext-секрет показывается админу один раз при генерации и нигде не хранится.
    /// </summary>
    public string? ClientSecretHash { get; set; }

    /// <summary>
    /// ; separated
    /// </summary>
    public string RedirectUris { get; set; } = ""; // newline или ; separated
    public string[] AllowedGrantTypes { get; set; } = { "authorization_code", "refresh_token", "password" }; // comma list
    public bool RequirePkce { get; set; } = true;
    public int AccessTokenLifetimeSeconds { get; set; } = 3600;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
    public bool AllowOfflineAccess { get; set; } = true;
    public string AllowedScopes { get; set; } = "openid profile email";

    public bool VerifySecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(ClientSecretHash))
            return false;

        try
        {
            return ApiKeyFormat.SecretMatchesHash(secret, ClientSecretHash);
        }
        catch (FormatException)
        {
            // значение не base64-хэш (старые plaintext-секреты не поддерживаются)
            return false;
        }
    }
}

// Authorization code storage
public class AuthCode
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string RedirectUri { get; set; } = default!;
    public string? State { get; set; }
    public Guid SubjectId { get; set; } // user id
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; } // S256 or plain
    public string Scopes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

// Refresh token storage
public class RefreshToken
{
    public Guid Id { get; set; }

    /// <summary>
    /// SHA-256 хэш токена (base64, контракт <see cref="ApiKeyFormat.HashSecret"/>) — plaintext не хранится.
    /// </summary>
    public string TokenHash { get; set; } = default!;

    public string ClientId { get; set; } = default!;
    public Guid SubjectId { get; set; } = default!; // user id
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}
