using Mars.Identity.Abstractions.Dto.Auth;
using Mars.Identity.Abstractions.Dto.Profile;
using Mars.Identity.Contracts.Users.UserProfiles;

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

    /// <summary>
    /// Пишет аватар пользователя в upload/Users/{userId}/ (полный 256×256 и миниатюра 64×64, webp).
    /// AvatarUrl в профиле сохраняется отдельным обновлением профиля.
    /// </summary>
    Task<AvatarUploadResponse> UploadAvatar(Guid userId, Stream image, string fileName, CancellationToken cancellationToken);
}
