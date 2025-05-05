using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Host.Services;

public interface ITokenService
{
    SigningCredentials GetSigningCredentials();
    Task<List<Claim>> GetClaimsAsync(UserEntity user, UserManager<UserEntity> userManager);
    JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    T? JwtDecode<T>(string payload, string? secret = null, bool verify = true);
    string JwtEncode(Dictionary<string, object> payload);
    string JwtEncode(Dictionary<string, object> payload, string secret);
}
