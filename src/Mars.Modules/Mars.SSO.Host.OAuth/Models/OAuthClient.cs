namespace Mars.SSO.Host.OAuth.Models;

// OAuthClient — зарегистрированные клиенты
public class OAuthClient
{
    //public Guid Id { get; set; }
    public string ClientId { get; set; } = default!;
    //public string? ClientSecretHash { get; set; } // hash secret для confidential clients
    public string? ClientSecret { get; set; } // hash secret для confidential clients

    /// <summary>
    /// ; separated
    /// </summary>
    public string RedirectUris { get; set; } = ""; // newline или ; separated
    public string AllowedGrantTypes { get; set; } = "authorization_code,refresh_token"; // comma list
    public bool RequirePkce { get; set; } = true;
    public int AccessTokenLifetimeSeconds { get; set; } = 3600;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
    public bool AllowOfflineAccess { get; set; } = true;
    public string AllowedScopes { get; set; } = "openid profile email";

    public bool VerifySecret(string secret) => ClientSecret == secret;
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
    public string Token { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public Guid SubjectId { get; set; } = default!; // user id
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}
