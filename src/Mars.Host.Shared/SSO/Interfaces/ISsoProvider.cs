using System.Security.Claims;
using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;

namespace Mars.Host.Shared.SSO.Interfaces;

public interface ISsoProvider
{
    string Name { get; }
    string DisplayName { get; }
    string? IconUrl { get; }

    /// <summary>
    /// Получить URL для авторизации (redirect на внешний сервис)
    /// </summary>
    string GetAuthorizationUrl(string state, string redirectUri, string? scope = null);

    /// <summary>
    /// Завершить аутентификацию по коду или токену от внешнего сервиса
    /// </summary>
    Task<SsoTokenResponse?> AuthenticateAsync(string code, string redirectUri);

    /// <summary>
    /// Проверяет валидность JWT токена (если провайдер работает через JWT)
    /// </summary>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);

    /// <summary>
    /// Преобразует ClaimsPrincipal в UpsertUserRemoteDataQuery (для создания или обновления пользователя).
    /// </summary>
    UpsertUserRemoteDataQuery MapToCreateUserQuery(ClaimsPrincipal principal);
}
