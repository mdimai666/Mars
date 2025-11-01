using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.Configuration;

namespace Mars.SSO.Host.OAuth.Services;

[Obsolete]
public class TokenService
{
    private readonly IKeyMaterialService _keys;
    private readonly IConfiguration _config;

    public TokenService(IKeyMaterialService keys, IConfiguration config)
    {
        _keys = keys;
        _config = config;
    }

    public string CreateIdToken(string userId, string clientId, string nonce)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("name", "Demo User"),
            new("preferred_username", "demo"),
            new("email", "demo@example.com")
        };

        var jwt = new JwtSecurityToken(
            issuer: _config["Auth:Issuer"],
            audience: clientId,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: _keys.GetSigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public string CreateAccessToken(string userId, string clientId)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("scope", "openid profile email")
        };

        var jwt = new JwtSecurityToken(
            issuer: _config["Auth:Issuer"],
            audience: clientId,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: _keys.GetSigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
