using Mars.Identity.Abstractions.Dto.Auth;
using Mars.Identity.Abstractions.Dto.Profile;

namespace Mars.Identity.Abstractions.Services;

public interface IAccountsService
{
    Task<AuthResultDto> Login(AuthCredentialsDto authCredentials, CancellationToken cancellationToken);
    Task<AuthResultDto> LoginForce(Guid userId, CancellationToken cancellationToken);
    Task<string?> FindPrefererUserName(string userInfoPrefererUsername);
    Task<RegistrationResponseDto> RegisterUser(UserForRegistrationQuery userData, CancellationToken cancellationToken);
    Task<UserProfileDto?> GetProfile(Guid userId, CancellationToken cancellationToken);
    Task Logout();
    Task<Guid?> ValidateUserCredentials(string username, string password, CancellationToken cancellationToken);
}
