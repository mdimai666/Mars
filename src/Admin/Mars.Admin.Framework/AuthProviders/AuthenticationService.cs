using Flurl.Http;
using Mars.Identity.Contracts.Auth;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.Framework.AuthProviders;

public class AuthenticationService : IAuthenticationService
{
    protected readonly IMarsWebApiClient _client;
    protected readonly CookieAuthStateProvider _authStateProvider;

    public AuthenticationService(IMarsWebApiClient client, CookieAuthStateProvider authStateProvider)
    {
        _client = client;
        _authStateProvider = authStateProvider;
    }

    /// <summary>
    /// Cookie-схема (A1): сервер ставит Identity-cookie в ответе (Set-Cookie),
    /// клиенту нечего сохранять — вызывающая страница перезагружается (forceLoad),
    /// и хост-страница приходит уже с авторизованным UserPrimaryInfo.
    /// </summary>
    public virtual Task<AuthResultResponse> Login(AuthCredentialsRequest userForAuthentication)
        => _client.Account.Login(userForAuthentication);

    public virtual async Task Logout()
    {
        try
        {
            await _client.Account.Logout();
        }
        catch (FlurlHttpException)
        {
            // сервер недоступен — снимаем только локальную сессию
        }

        await _authStateProvider.MarkUserAsLoggedOut();
    }

    public virtual async Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userForRegistration)
    {
        var registrationResult = await _client.Account.RegisterUser(userForRegistration);

        return registrationResult!;
    }

}
