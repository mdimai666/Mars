using System.Net.Http.Headers;
using AppFront.Shared.Tools;
using Blazored.LocalStorage;
using Mars.Shared.Contracts.Auth;
using Mars.Shared.Contracts.SSO;
using Mars.WebApiClient.Interfaces;
using Microsoft.JSInterop;

namespace AppFront.Shared.AuthProviders;

public class AuthenticationService : IAuthenticationService
{
    protected readonly IMarsWebApiClient _client;
    protected readonly CookieOrLocalStorageAuthStateProvider _authStateProvider;
    protected readonly ILocalStorageService _localStorage;
    protected MyJS _js;

    public AuthenticationService(IMarsWebApiClient client, CookieOrLocalStorageAuthStateProvider authStateProvider, ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _client = client;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
        _js = new MyJS(jsRuntime);
    }

    public virtual async Task<AuthResultResponse> Login(AuthCreditionalsRequest userForAuthentication)
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
        await _js.CookieRemove(".AspNetCore.Identity.Application");
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
