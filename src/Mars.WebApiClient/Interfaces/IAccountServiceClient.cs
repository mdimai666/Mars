using Mars.Identity.Contracts.Auth;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Contracts.Users.UserProfiles;

namespace Mars.WebApiClient.Interfaces;

public interface IAccountServiceClient
{
    Task<AuthResultResponse> Login(AuthCredentialsRequest authCredentials);
    Task Logout();
    Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userData);

    /// <summary>Модель редактирования профиля текущего пользователя (метаполя + форма).</summary>
    Task<UserProfileEditViewModel> GetProfileEditModel();

    /// <summary>Обновление профиля текущего пользователя (whitelist-поля + метаполя).</summary>
    Task<UserDetailResponse> UpdateProfile(UpdateUserProfileRequest request);

    /// <summary>Загрузка аватара (полный 256×256 + миниатюра 64×64 в upload/Users/{id}/). userId — только для админа.</summary>
    Task<AvatarUploadResponse> UploadAvatar(Stream image, string fileName, Guid? userId = null);
}
