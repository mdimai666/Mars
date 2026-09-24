using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Flurl.Http;
using Mars.Identity.Contracts.Auth;
using Mars.SSO.Contracts.Dto;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.Framework.AuthProviders;

public class AuthenticationService : IAuthenticationService
{
    protected readonly IMarsWebApiClient _client;
    protected readonly CookieOrLocalStorageAuthStateProvider _authStateProvider;
    protected readonly ILocalStorageService _localStorage;

    public AuthenticationService(IMarsWebApiClient client, CookieOrLocalStorageAuthStateProvider authStateProvider, ILocalStorageService localStorage)
    {
        _client = client;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
    }

    public virtual async Task<AuthResultResponse> Login(AuthCredentialsRequest userForAuthentication)
    {
        var result = await _client.Account.Login(userForAuthentication);

        if (!result.IsAuthSuccessful) return result;

        await LoginStage(result);

        return new AuthResultResponse { ErrorMessage = null };
    }

    public virtual Task MarkUserAsAuthenticated(string token, SsoUserInfoResponse? ssoUserInfo = null)
    {
        return _authStateProvider.MarkUserAsAuthenticated(token, ssoUserInfo);
    }

    public virtual async Task LoginCallback(AuthResultResponse authData)
    {
        await LoginStage(authData);
    }

    protected virtual async Task LoginStage(AuthResultResponse result)
    {
        ArgumentNullException.ThrowIfNull(result.Token, nameof(result.Token));
        await _localStorage.SetItemAsync("authToken", result.Token);
        await _authStateProvider.MarkUserAsAuthenticated(result.Token, null);
        _client.Client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.Token);
    }

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

        await _localStorage.RemoveItemAsync("authToken");
        await _authStateProvider.MarkUserAsLoggedOut();
        _client.Client.HttpClient.DefaultRequestHeaders.Authorization = null;
        Q.LogoutUser();
    }

    public virtual async Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userForRegistration)
    {
        var registrationResult = await _client.Account.RegisterUser(userForRegistration);

        return registrationResult!;
    }

}
