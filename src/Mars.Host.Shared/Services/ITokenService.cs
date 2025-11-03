using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Host.Shared.Services;

public interface ITokenService
{
    int ExpiryInMinutes { get; }
    int ExpiryInSeconds { get; }

    SigningCredentials GetSigningCredentials();
    List<Claim> GetClaims(AuthorizedUserInformationDto user);
    JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    Task<string> CreateAccessToken(Guid userId, IUserRepository userRepository, CancellationToken cancellationToken);
    long JwtExpireUnixSeconds();
    ClaimsPrincipal? ValidateToken(string token);
}
