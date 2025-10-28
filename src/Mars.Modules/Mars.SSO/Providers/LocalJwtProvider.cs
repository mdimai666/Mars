using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Mars.SSO.Providers;

public class LocalJwtProvider : ISsoProvider
{
    private readonly string _name = "local";
    private readonly string _display = "Local JWT";
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _signingKey;
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(1);

    public LocalJwtProvider(IConfiguration config)
    {
        _issuer = config["LocalJwt:Issuer"] ?? "https://my.local";
        _audience = config["LocalJwt:Audience"] ?? _issuer;
        var key = config["LocalJwt:Key"] ?? throw new InvalidOperationException("LocalJwt:Key not configured");
        _signingKey = Encoding.UTF8.GetBytes(key);
    }

    public string Name => _name;
    public string DisplayName => _display;
    public string? IconUrl => ""; //TODO: setup

    public string GetAuthorizationUrl(string state, string redirectUri, string? scope = null)
        => throw new NotSupportedException("Local provider does not support interactive auth");

    public Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri)
        => throw new NotSupportedException("Local provider does not support authorization code flow");

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        var tvp = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(_signingKey),
            ValidateLifetime = true
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, tvp, out _);
        }
        catch { return null; }
    }

    // helper — issue a token for local users
    public string IssueToken(string subject, IEnumerable<Claim>? claims = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var signing = new SigningCredentials(new SymmetricSecurityKey(_signingKey), SecurityAlgorithms.HmacSha256);
        var jwt = handler.CreateJwtSecurityToken(
        issuer: _issuer,
        audience: _audience,
        subject: new ClaimsIdentity(claims ?? new[] { new Claim(ClaimTypes.NameIdentifier, subject) }),
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.Add(_tokenLifetime),
        signingCredentials: signing
        );
        return handler.WriteToken(jwt);
    }

    public UpsertUserRemoteDataQuery MapToCreateUserQuery(ClaimsPrincipal principal)
        => throw new NotSupportedException("Local provider does not support MapToCreateUserQuery");
}
