using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Host.Shared.Services;

public interface ITokenService
{
    SigningCredentials GetSigningCredentials();
    List<Claim> GetClaims(AuthorizedUserInformationDto user);
    JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    T? JwtDecode<T>(string payload, string? secret = null, bool verify = true);
    string JwtEncode(Dictionary<string, object> payload);
    string JwtEncode(Dictionary<string, object> payload, string secret);
    Task<string> CreateToken(Guid userId, IUserRepository userRepository, CancellationToken cancellationToken);
    long JwtExpireUnixSeconds();
    ClaimsPrincipal? ValidateToken(string token);
}
