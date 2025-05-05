using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mars.Host.Models;
using Mars.Test.Common.Constants;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Integration.Tests.Common;

public class TokenGenerator
{
    private readonly JwtSecurityTokenHandler s_tokenHandler = new();

    private static JwtSettings s_jwtSettings = new JwtSettings()
    {
        ExpiryInMinutes = 43200,
        SecurityKey = "MarsSuperSecretKey256greaterThan32",
        ValidAudience = "https://localhost:5003",
        ValidIssuer = "MarsIssuerAPI"
    };

    public SigningCredentials SigningCredentials { get; }

    public TokenGenerator()
    {
        var key = Encoding.UTF8.GetBytes(s_jwtSettings.SecurityKey);
        var secret = new SymmetricSecurityKey(key);
        SigningCredentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    public string GenerateJwtToken(IEnumerable<Claim> claims) =>
        s_tokenHandler.WriteToken(new JwtSecurityToken(s_jwtSettings.ValidIssuer,
                                                        s_jwtSettings.ValidAudience,
                                                        claims, null, DateTime.UtcNow.AddMinutes(20),
                                                        SigningCredentials));

    public string GenerateTokenWithClaims(string[]? roles = null)
    {
        var user = UserConstants.TestUser;

        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user?.Email??""),
            // https://learn.microsoft.com/ru-ru/dotnet/api/microsoft.aspnetcore.identity.identityuser-1.securitystamp?view=aspnetcore-8.0#definition
            new Claim("AspNet.Identity.SecurityStamp", user.SecurityStamp),
            new Claim(ClaimTypes.GivenName, user.FirstName??""),
            new Claim(ClaimTypes.Surname, user.LastName??""),
        };

        foreach (var role in roles ?? user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return GenerateJwtToken(claims);
    }
}
