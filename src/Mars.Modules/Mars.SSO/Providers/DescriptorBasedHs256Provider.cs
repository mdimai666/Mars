using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Mars.SSO.Providers;

/// <summary>
/// small adapter that validates HS256 tokens using provided key
/// </summary>
public class DescriptorBasedHs256Provider : ISsoProvider
{
    private readonly SsoProviderDescriptor _d;
    private readonly byte[] _key;

    public DescriptorBasedHs256Provider(SsoProviderDescriptor d)
    {
        _d = d;
        _key = Encoding.UTF8.GetBytes(d.SigningKey ?? throw new InvalidOperationException("SigningKey required"));
    }

    public string Name => _d.Name;
    public string DisplayName => _d.DisplayName ?? _d.Name;
    public string? IconUrl => _d.IconUrl;

    public string GetAuthorizationUrl(string state, string redirectUri, string? scope = null)
    => throw new NotSupportedException();

    public Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri)
        => Task.FromResult<SsoTokenResponse?>(null);

    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        var tvp = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(_d.Issuer),
            ValidIssuer = _d.Issuer,
            ValidateAudience = !string.IsNullOrEmpty(_d.ClientId),
            ValidAudiences = string.IsNullOrEmpty(_d.ClientId) ? null : new[] { _d.ClientId },
            IssuerSigningKey = new SymmetricSecurityKey(_key),
            ValidateLifetime = true
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return Task.FromResult(handler.ValidateToken(token, tvp, out _))!;
        }
        catch { return Task.FromResult<ClaimsPrincipal?>(null); }
    }

    public UpsertUserRemoteDataQuery MapToCreateUserQuery(ClaimsPrincipal principal)
        => throw new NotSupportedException("DescriptorBasedHs256Provider does not support MapToCreateUserQuery");
}
