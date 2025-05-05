using System.ComponentModel.DataAnnotations;

namespace Mars.Host.Models;

public class JwtSettings
{
    public const string JwtSectionKey = "JWTSettings";

    [MinLength(32)]
    public required string SecurityKey { get; set; }
    public required string ValidIssuer { get; set; }
    [Url]
    public required string ValidAudience { get; set; }
    public int ExpiryInMinutes { get; set; }
}
