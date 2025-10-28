using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using AppFront.Shared.AuthProviders;
using AppFront.Shared.Interfaces;
using Flurl.Http;
using Mars.Core.Exceptions;
using Mars.Core.Utils;
using Mars.Options.Models;
using Mars.Shared.Contracts.Auth;
using Mars.Shared.Contracts.SSO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AppAdmin.Pages.Public;

public partial class LoginForm
{
    [CascadingParameter] public Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject] IAuthenticationService _authenticationService { get; set; } = default!;
    [Inject] NavigationManager _navigationManager { get; set; } = default!;
    [Inject] IMessageService _messageService { get; set; } = default!;

    [Inject] IFlurlClient _client { get; set; } = default!;

    [Parameter] public string AfterLoginUrl { get; set; } = "/dev";
    private AuthCreditionalsModel auth = new();

    public bool ShowAuthError { get; set; }
    public string? Error;
    public string? _successAlertMessage;

    private bool _isAlreadyAuth;
    private bool _loginOverlayVisible;

    public string AuthProvider { get; set; } = "";

    //[SupplyParameterFromQuery] //
    public string? ReturnUrl;
    //[SupplyParameterFromQuery] //парсится не верно
    public string? Data;

    bool IsLoginLoading = false;

    AuthVariantConstOption authVariantConstOption = default!;

    protected override void OnInitialized()
    {
        Load();
    }

    bool IsLoginAsSSOService = false;

    async void Load()
    {
        base.OnInitialized();

        var authState = await AuthState;

        _isAlreadyAuth = authState.User?.Identity?.IsAuthenticated ?? false;

        if (_isAlreadyAuth)
        {
            _navigationManager.NavigateTo(AfterLoginUrl);
        }

        authVariantConstOption = Q.Site.GetRequiredOption<AuthVariantConstOption>();

        if (DetectIsSsoAuthProcessingAndUrlHasStateCode())
        {
            Console.WriteLine("DetectIsSsoAuthProcessingAndUrlHasStateCode");
            await SsoAuthProcessingCallback();
        }
        Console.WriteLine("LoginForm.End");

        return;

        var querystring = HttpUtility.ParseQueryString(new Uri(_navigationManager.Uri).Query);

        Data = querystring["data"];
        ReturnUrl = querystring["returnurl"];
        var openID_state = querystring["state"];
        var openID_code = querystring["code"];
        var openID_sso = querystring["sso"];
        var openID_client_id = querystring["client_id"];

#if DEBUG
        Console.WriteLine($"Data={Data}");
        Console.WriteLine($"ReturnUrl={ReturnUrl}");
#endif

        if (!string.IsNullOrEmpty(Data))
        {
            IsLoginLoading = true;
            AuthProvider = "esia";
            StateHasChanged();
            SendThirdAuthDataToServer();
        }
        else if (!string.IsNullOrEmpty(openID_state) && !string.IsNullOrEmpty(openID_code) && !string.IsNullOrEmpty(openID_sso))
        {
            ThirdLogin(openID_sso);
        }
        else if (!string.IsNullOrEmpty(openID_client_id) && !string.IsNullOrEmpty(openID_sso))
        {
            IsLoginAsSSOService = true;
        }
    }

    public async Task ExecuteLogin()
    {
        if (IsLoginAsSSOService)
        {
            try
            {
                var auth2 = await SendOpenIDLogin(auth.Login, auth.Password);

                if (auth2.Action == AuthStepAction.Error)
                {
                    Error = auth2.ErrorMessage;
                    ShowAuthError = true;
                    StateHasChanged();
                }
                else
                {
                    if (string.IsNullOrEmpty(auth2.RedirectUrl))
                    {
                        throw new ArgumentNullException(nameof(auth2.RedirectUrl), "auth2.RedirectUrl is null");
                    }
                    _navigationManager.NavigateTo(auth2.RedirectUrl, true);
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                ShowAuthError = true;
                StateHasChanged();
            }
            return;
        }

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
            await Task.Delay(10);//из-за редиректа какя то бага и не переходит по ссылке

            if (string.IsNullOrEmpty(ReturnUrl))
            {
                _navigationManager.NavigateTo(AfterLoginUrl);
            }
            else
            {
                _navigationManager.NavigateTo(ReturnUrl);
            }
        }
    }

    public async void ThirdLogin(string ssoName)
    {
        IsLoginLoading = true;
        ShowAuthError = false;
        StateHasChanged();

        var querystring = HttpUtility.ParseQueryString(new Uri(_navigationManager.Uri).Query);
        AuthenticationService authenticationService = (_authenticationService as AuthenticationService)!;
        var res = await authenticationService.SSOLogin(ssoName, ReturnUrl, _navigationManager.Uri.Split('?')[0], querystring.ToString()!);

        if (res.Action == AuthStepAction.Redirect)
        {
            _navigationManager.NavigateTo(res.RedirectUrl, true);
        }
        else if (res.Action == AuthStepAction.Error)
        {
            Error = res.ErrorMessage;
            ShowAuthError = true;
            IsLoginLoading = false;
            StateHasChanged();

        }
        else if (res.Action == AuthStepAction.Complete)
        {
            await authenticationService.LoginCallback(res.AuthResultResponse!);

            if (string.IsNullOrEmpty(ReturnUrl) == false)
            {
                _navigationManager.NavigateTo(ReturnUrl);
            }
            else
            {
                _navigationManager.NavigateTo(AfterLoginUrl);
            }
        }
    }

    public void SsoProviderLogin(string ssoSlug)
    {
        _loginOverlayVisible = true;
        StateHasChanged();

        // see SsoController
        //var redirectUri = HttpUtility.UrlEncode(NavigationManager.Uri.Split('?')[0]);
        //var redirectUri = HttpUtility.UrlEncode(NavigationManager.Uri.Split('?')[0] + "?provider=" + ssoSlug);
        var redirectUri = (_navigationManager.Uri.Split('?')[0] + "?provider=" + ssoSlug);
        Console.WriteLine("redirectUri=" + redirectUri);
        var url = UrlTool.Combine(_client.BaseUrl.ToString(), "/api/sso/login", ssoSlug) + "?redirectUri=" + redirectUri;
        _navigationManager.NavigateTo(url);

        // http://localhost:5003/dev/Login?returnUrl=http://localhost:5003/dev/Login&state=6cab21716a5840ada5e8a26ee3f79c20&session_state=880247b9-b36f-4404-b9e3-d179657e7a7f&iss=http%3A%2F%2Flocalhost%3A6767%2Frealms%2Fmyrealm&code=58de7b26-2c04-44a5-a710-e8b965ae3d0c.880247b9-b36f-4404-b9e3-d179657e7a7f.40143acd-6972-45aa-965a-39fa3b33b0b5

        _loginOverlayVisible = false;
        StateHasChanged();
    }

    bool DetectIsSsoAuthProcessingAndUrlHasStateCode()
    {
        var querystring = HttpUtility.ParseQueryString(new Uri(_navigationManager.Uri).Query);
        var code = querystring["code"];
        var provider = querystring["provider"];

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
        var provider = querystring["provider"];
        //var redirectUri = NavigationManager.Uri.Split('?')[0]; ;
        //var redirectUri = HttpUtility.UrlEncode(NavigationManager.Uri.Split('?')[0] + "?provider=" + provider);
        var redirectUri = (_navigationManager.Uri.Split('?')[0] + "?provider=" + provider);// должно строго совподать с redirectUri которую передали при получении кода
        Console.WriteLine("redirectUri=" + redirectUri);

        try
        {
            var userInfo = await _client.Request("/api/sso/callback", provider).AppendQueryParam(new { code, redirectUri }).GetJsonAsync<SsoUserInfoResponse>();
            //var token = userInfo.RawData["access_token"];
            _successAlertMessage = "Авторизировано";
            //_loginOverlayVisible = false;
            //StateHasChanged();

            //await Task.Delay(1000);

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
            await _authenticationService.MarkUserAsAuthenticated(auth.AccessToken, auth);
            await Task.Delay(200);
            _navigationManager.NavigateTo(AfterLoginUrl);
        }

        _loginOverlayVisible = false;
        StateHasChanged();
    }

    async void SendThirdAuthDataToServer()
    {
        AuthenticationService authenticationService = (_authenticationService as AuthenticationService)!;

        if (Data is null) throw new ArgumentNullException(nameof(Data));

        var result = await authenticationService.ThirdLogin(AuthProvider, Data);
        IsLoginLoading = false;

        if (result.Ok)
        {
            _ = _messageService.Success("success");
            _navigationManager.NavigateTo(ReturnUrl ?? "/");

        }
        else
        {
            ShowAuthError = true;
            Error = result.Message;
            //_ = messageService.Error(result.Message);
        }
        StateHasChanged();
    }

    async Task<AuthStepsResponse> SendOpenIDLogin(string username, string password)
    {
        var querystring = HttpUtility.ParseQueryString(new Uri(_navigationManager.Uri).Query);

        var client_id = querystring["client_id"]!;
        var redirect_uri = querystring["redirect_uri"]!;
        var state = querystring["state"]!;
        var scope = querystring["scope"]!;

        var body = new List<KeyValuePair<string, string>>
        {
            new ("client_id", client_id),
            new ("redirect_uri", redirect_uri),
            new ("response_type", "code"),
            new ("state", state),
            new ("scope", scope),
            new ("grant_type", "authorization_code"),
            new ("username", username),
            new ("password", password),
        };

        var res = await SendForm<AuthStepsResponse>(body, "/api/OAuth/Login?" + querystring);

        return res;
    }

    async Task<T> SendForm<T>(List<KeyValuePair<string, string>> body, string requestUri)
    {
        var form = new FormUrlEncodedContent(body).ReadAsStringAsync().Result;
        var post = new StringContent(form, Encoding.UTF8, "application/x-www-form-urlencoded");
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = post
        };

        var req = await _client.HttpClient.SendAsync(request);

        if (req.IsSuccessStatusCode)
        {
            //return JsonSerializer.Deserialize<T>(await req.Content.ReadAsStringAsync())!;
            var b = await req.Content.ReadAsStringAsync();
            var j = JsonSerializer.Deserialize<T>(b, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            return j;
        }
        else
        {
            throw new Exception(req.ReasonPhrase ?? req.StatusCode.ToString());
        }
    }

    class AuthCreditionalsModel
    {
        [Required(ErrorMessage = "Заполните Логин/Почту")]
        public string Login { get; set; } = "";
        [Required(ErrorMessage = "Заполните Пароль")]
        public string Password { get; set; } = "";

        public AuthCreditionalsRequest ToRequest() => new() { Login = Login, Password = Password };
    }
}
