using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mars.Host.Models;
using Mars.Host.Shared.Dto.Users;
using Mars.Test.Common.Constants;

namespace Mars.Integration.Tests.Common;

public class TokenGenerator
{
    private readonly JwtSecurityTokenHandler s_tokenHandler = new();

    public TestKeyMaterialService KeyMaterialService;

    private static JwtSettings s_jwtSettings = new()
    {
        ExpiryInMinutes = 43200,
        ValidAudience = "mars-app",
    };

    private string _validIssuer = "http://localhost";

    public TokenGenerator()
    {
        KeyMaterialService = new TestKeyMaterialService();
    }

    public string GenerateJwtToken(IEnumerable<Claim> claims) =>
        s_tokenHandler.WriteToken(new JwtSecurityToken(_validIssuer,
                                                        s_jwtSettings.ValidAudience,
                                                        claims, null, DateTime.UtcNow.AddMinutes(20),
                                                        KeyMaterialService.GetSigningCredentials()));

    public string GenerateTokenWithClaims(string[]? roles = null)
    {
        return GenerateTokenWithClaims(UserConstants.TestUser, UserConstants.TestUser.SecurityStamp, roles);
    }

    public string GenerateTokenWithClaims(UserDetail user, string securityStamp, string[]? roles = null)
    {
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user?.Email??""),
            // https://learn.microsoft.com/ru-ru/dotnet/api/microsoft.aspnetcore.identity.identityuser-1.securitystamp?view=aspnetcore-8.0#definition
            new("AspNet.Identity.SecurityStamp", securityStamp),
            new(ClaimTypes.GivenName, user.FirstName??""),
            new(ClaimTypes.Surname, user.LastName??""),
        };

        foreach (var role in roles ?? user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return GenerateJwtToken(claims);
    }
}
