using System.Net.Http.Headers;
using System.Security.Claims;
using AppFront.Shared.Features;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace AppFront.Shared.AuthProviders;

public class AuthStateProvider : AuthenticationStateProvider
{

    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationState _anonymous;

    public AuthStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));


    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (Q.IsPrerender)
        {
            return new AuthenticationState(new ClaimsPrincipal());
        }

        //Console.WriteLine("GetAuthenticationStateAsync");
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (string.IsNullOrEmpty(token))
            return _anonymous;

        Q.AuthToken = token;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var ci = new ClaimsPrincipal(new ClaimsIdentity(
            JwtParser.ParseClaimsFromJwt(token), "jwtAuthType"
        ));
        AuthenticationState state = new AuthenticationState(ci);

        Q.UpdateClaimUser(ci);

        return state;
    }

    public void NotifyUserAuthentication(string token)
    {
        var ci = new ClaimsPrincipal(new ClaimsIdentity(JwtParser.ParseClaimsFromJwt(token), "jwtAuthType"));
        var authenticatedUser = ci;
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));

        Q.AuthToken = token;
        Q.UpdateClaimUser(ci);

        //Console.WriteLine("ee>>" + JsonConvert.SerializeObject(ci));

        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(_anonymous);
        NotifyAuthenticationStateChanged(authState);
    }

}
