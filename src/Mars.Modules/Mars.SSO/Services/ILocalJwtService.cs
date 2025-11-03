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

    public MarsLocalJwtService(ITokenService tokenService, IUserRepository userRepository)
    {
        _tokenService = tokenService;
        _userRepository = userRepository;
    }

    public Task<string> CreateToken(Guid userId, CancellationToken cancellationToken)
    {
        return _tokenService.CreateAccessToken(userId, _userRepository, cancellationToken);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        return _tokenService.ValidateToken(token);
    }
}
