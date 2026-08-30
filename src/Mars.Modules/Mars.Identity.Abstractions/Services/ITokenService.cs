using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mars.Identity.Abstractions.Dto.Users;
using Mars.Identity.Abstractions.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Identity.Abstractions.Services;

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
