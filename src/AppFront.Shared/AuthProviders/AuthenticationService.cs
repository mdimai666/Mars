using System.Net.Http.Headers;
using AppFront.Shared.Tools;
using Blazored.LocalStorage;
using Flurl.Http;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Auth;
using Mars.Shared.Contracts.SSO;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace AppFront.Shared.AuthProviders;

public class AuthenticationService : IAuthenticationService
{
    protected readonly IMarsWebApiClient _client;
    protected readonly AuthenticationStateProvider _authStateProvider;
    protected readonly ILocalStorageService _localStorage;
    protected MyJS _js;

    public AuthenticationService(IMarsWebApiClient client, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage, IJSRuntime jsRuntime)
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

    public virtual async Task Login(AuthResultResponse authData)
    {
        await LoginStage(authData);
    }

    protected virtual async Task LoginStage(AuthResultResponse result)
    {
        ArgumentNullException.ThrowIfNull(result.Token, nameof(result.Token));
        await _localStorage.SetItemAsync("authToken", result.Token);
        ((AuthStateProvider)_authStateProvider).NotifyUserAuthentication(result.Token);
        _client.Client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.Token);
    }

    public virtual async Task Logout()
    {
        await _js.CookieRemove(".AspNetCore.Identity.Application");
        await _localStorage.RemoveItemAsync("authToken");
        ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
        _client.Client.HttpClient.DefaultRequestHeaders.Authorization = null;
        Q.LogoutUser();
    }

    public virtual async Task<RegistrationResultResponse> RegisterUser(UserForRegistrationRequest userForRegistration)
    {
        var registrationResult = await _client.Account.RegisterUser(userForRegistration);

        return registrationResult!;
    }

    public virtual async Task<UserActionResult> ThirdLogin(string serviceName, string token)
    {
        try
        {
            var result = await _client.Client.Request("/api/Account/EsiaLogin").PostJsonAsync(new[] { token }).ReceiveJson<AuthResultResponse>();

            if (result?.IsAuthSuccessful ?? false)
            {
                await LoginStage(result);
            }

            return new UserActionResult
            {
                Ok = result?.IsAuthSuccessful ?? false,
                Message = result?.ErrorMessage ?? ""
            };

        }
        catch (Exception ex)
        {
            return new UserActionResult
            {
                Ok = false,
                Message = ex.Message,
            };
        }
    }

    public Task<AuthStepsResponse> SSOLogin(string ssoName, string? returnUrl, string redirectUrl, string? querystring)
    {
        return _client.Client.Request($"/api/OAuth/SSOLogin?ssoName={ssoName}&returnUrl={returnUrl}&redirectUrl={redirectUrl}&{querystring}")
            .PostJsonAsync(new string[] { }).ReceiveJson<AuthStepsResponse>();
    }

}
