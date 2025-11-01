using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;

namespace Mars.SSO.Services;

public interface ILocalJwtService
{
    Task<string> CreateToken(Guid userId, CancellationToken cancellationToken);
    ClaimsPrincipal? ValidateToken(string token);
}

public class MarsLocalJwtService : ILocalJwtService
{
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    //private readonly IServiceScopeFactory _serviceScopeFactory;

    public MarsLocalJwtService(ITokenService tokenService, IUserRepository userRepository)
    {
        _ = nameof(ITokenService);
        _tokenService = tokenService;
        _userRepository = userRepository;
        //_serviceScopeFactory = serviceScopeFactory;
    }

    //public string CreateToken(Guid userId, string email, IEnumerable<string>? roles, CancellationToken cancellationToken)
    public Task<string> CreateToken(Guid userId, CancellationToken cancellationToken)
    {
        //using var scope = _serviceScopeFactory.CreateScope();
        //var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        return _tokenService.CreateAccessToken(userId, _userRepository, cancellationToken);

        /*
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
        };

        if (roles != null)
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
        */
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        return _tokenService.ValidateToken(token);
        /*
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }*/
    }
}
