using System.ComponentModel.DataAnnotations;
using System.Web;
using Flurl.Http;
using Mars.Admin.Framework.AuthProviders;
using Mars.Admin.Framework.Interfaces;
using Mars.Core.Exceptions;
using Mars.Core.Utils;
using Mars.Identity.Contracts.Auth;
using Mars.Identity.Contracts.Options;
using Mars.SSO.Contracts.Dto;
using Mars.SSO.Contracts.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Mars.Admin.Pages.Public;

public partial class LoginForm
{
    [CascadingParameter] public Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject] IAuthenticationService _authenticationService { get; set; } = default!;
    [Inject] NavigationManager _navigationManager { get; set; } = default!;
    [Inject] IMessageService _messageService { get; set; } = default!;
    [Inject] PasskeyJs _passkeyJs { get; set; } = default!;

    [Inject] IFlurlClient _client { get; set; } = default!;

    [Parameter] public string AfterLoginUrl { get; set; } = "/dev";
    [Parameter] public string? ProviderPassedInUriPart { get; set; }

    private AuthCredentialsModel auth = new();

    public bool ShowAuthError { get; set; }
    public string? Error;
    public string? _successAlertMessage;

    private bool _isAlreadyAuth;
    private bool _loginOverlayVisible;
    private bool _passkeyAvailable;

    public string AuthProvider { get; set; } = "";

    //[SupplyParameterFromQuery] //
    public string? ReturnUrl;
    //[SupplyParameterFromQuery] //парсится не верно
    public string? Data;

    bool IsLoginLoading = false;

    AuthVariantConstOption? authVariantConstOption = default!;

    protected override void OnInitialized()
    {
        Load();
    }

    async void Load()
    {
        base.OnInitialized();

        var authState = await AuthState;

        _isAlreadyAuth = authState.User?.Identity?.IsAuthenticated ?? false;

        if (_isAlreadyAuth)
        {
            _navigationManager.NavigateTo(AfterLoginUrl);
        }

        authVariantConstOption = Q.Site.GetOption<AuthVariantConstOption>();

        _passkeyAvailable = Q.Site.GetOption<PasskeyOption>()?.Enabled != false && await _passkeyJs.IsAvailable();
        StateHasChanged();

        if (DetectIsSsoAuthProcessingAndUrlHasStateCode())
        {
            Console.WriteLine("DetectIsSsoAuthProcessingAndUrlHasStateCode");
            await SsoAuthProcessingCallback();
        }
        Console.WriteLine("LoginForm.End");

        return;
    }

    public async Task ExecuteLogin()
    {
        ShowAuthError = false;
        var result = await _authenticationService.Login(auth.ToRequest());
        if (!result.IsAuthSuccessful)
        {
            Error = result.ErrorMessage;
            ShowAuthError = true;
            StateHasChanged();
        }
        else
        {
            // cookie-схема (A1): сессия уже установлена Set-Cookie в ответе сервера —
            // полная перезагрузка, чтобы хост-страница отрендерилась авторизованной
            _navigationManager.NavigateTo(string.IsNullOrEmpty(ReturnUrl) ? AfterLoginUrl : ReturnUrl, forceLoad: true);
        }
    }

    public async Task ExecutePasskeyLogin()
    {
        Error = null;
        ShowAuthError = false;
        _loginOverlayVisible = true;
        StateHasChanged();

        try
        {
            var result = await _passkeyJs.LoginWithPasskey();

            if (result.IsAuthSuccessful)
            {
                _navigationManager.NavigateTo(string.IsNullOrEmpty(ReturnUrl) ? AfterLoginUrl : ReturnUrl, forceLoad: true);
                return;
            }

            Error = result.ErrorMessage ?? "Ошибка входа по пасскею";
            ShowAuthError = true;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            ShowAuthError = true;
        }
        finally
        {
            _loginOverlayVisible = false;
            StateHasChanged();
        }
    }

    string GenerateRedirectUri(string ssoSlug)
    {
        var providerDriver = authVariantConstOption.SSOConfigs.FirstOrDefault(s => s.Slug == ssoSlug)?.Driver;
        string redirectUri;
        if (providerDriver == "microsoft")
            redirectUri = UrlTool.Combine(_navigationManager.BaseUri, "/Login/sso/", ssoSlug);
        else
            redirectUri = (_navigationManager.Uri.Split('?', 2)[0] + "?provider=" + ssoSlug);
        return redirectUri;
    }

    public void SsoProviderLogin(string ssoSlug)
    {
        _loginOverlayVisible = true;
        StateHasChanged();

        // see SsoController
        var redirectUri = GenerateRedirectUri(ssoSlug);
        Console.WriteLine("redirectUri=" + redirectUri);
        string url = UrlTool.Combine(_client.BaseUrl.ToString(), "/api/sso/login", ssoSlug) + "?redirectUri=" + redirectUri;

        _navigationManager.NavigateTo(url);

        // http://localhost:5003/dev/Login?returnUrl=http://localhost:5003/dev/Login&state=6cab21716a5840ada5e8a26ee3f79c20&session_state=880247b9-b36f-4404-b9e3-d179657e7a7f&iss=http%3A%2F%2Flocalhost%3A6767%2Frealms%2Fmyrealm&code=58de7b26-2c04-44a5-a710-e8b965ae3d0c.880247b9-b36f-4404-b9e3-d179657e7a7f.40143acd-6972-45aa-965a-39fa3b33b0b5

        _loginOverlayVisible = false;
        StateHasChanged();
    }

    bool DetectIsSsoAuthProcessingAndUrlHasStateCode()
    {
        var querystring = HttpUtility.ParseQueryString(new Uri(_navigationManager.Uri).Query);
        var code = querystring["code"];
        var provider = ProviderPassedInUriPart ?? querystring["provider"];

        Console.WriteLine("code=" + code + "& provider=" + provider);

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(provider)) return false;

        return true;
    }

    public async Task<SsoUserInfoResponse?> ExchangeProvidedCodeToToken()
    {
        Error = null;
        ShowAuthError = false;
        StateHasChanged();

        Console.WriteLine("ExchangeProvidedCodeToToken");
        var querystring = HttpUtility.ParseQueryString(new Uri(_navigationManager.Uri).Query);
        var code = querystring["code"];
        var provider = ProviderPassedInUriPart ?? querystring["provider"]!;
        var redirectUri = GenerateRedirectUri(provider); ;// должно строго совподать с redirectUri которую передали при получении кода
        Console.WriteLine("redirectUri=" + redirectUri);

        try
        {
            var userInfo = await _client.Request("/api/sso/callback", provider).AppendQueryParam(new { code, redirectUri }).GetJsonAsync<SsoUserInfoResponse>();
            //var token = userInfo.RawData["access_token"];
            _successAlertMessage = "Авторизировано";

            return userInfo;
        }
        catch (UnauthorizedException)
        {
            Error = "ExchangeCodeForToken error";
            ShowAuthError = true;
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            Error = "ExchangeCodeForToken error";
            ShowAuthError = true;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            ShowAuthError = true;
        }
        return null;

        StateHasChanged();
    }

    public async Task SsoAuthProcessingCallback()
    {
        _loginOverlayVisible = true;
        StateHasChanged();

        var auth = await ExchangeProvidedCodeToToken();

        if (auth == null)
        {

        }
        else
        {
            // серверный callback уже поставил Identity-cookie — полная перезагрузка
            _navigationManager.NavigateTo(AfterLoginUrl, forceLoad: true);
        }

        _loginOverlayVisible = false;
        StateHasChanged();
    }

    class AuthCredentialsModel
    {
        [Required(ErrorMessage = "Заполните Логин/Почту")]
        public string Login { get; set; } = "";
        [Required(ErrorMessage = "Заполните Пароль")]
        public string Password { get; set; } = "";

        public AuthCredentialsRequest ToRequest() => new() { Login = Login, Password = Password };
    }
}
