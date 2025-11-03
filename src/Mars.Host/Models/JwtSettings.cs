namespace Mars.Host.Models;

public class JwtSettings
{
    public const string JwtSectionKey = "JWTSettings";
    public required string ValidAudience { get; set; }
    public int ExpiryInMinutes { get; set; }
    public string PrivateKeyPath { get; set; } = "data/jwt_private.pem";
}
