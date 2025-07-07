using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Web;
using AppFront.Shared.AuthProviders;
using AppFront.Shared.Interfaces;
using Mars.Options.Models;
using Mars.Shared.Contracts.Auth;
using Mars.Shared.Contracts.SSO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AppAdmin.Pages.Public;

public partial class LoginForm
{
    [CascadingParameter] public Task<AuthenticationState> AuthState { get; set; } = default!;

    class AuthCreditionalsModel
    {
        [Required(ErrorMessage = "Заполните Логин/Почту")]
        public string Login { get; set; } = "";
        [Required(ErrorMessage = "Заполните Пароль")]
        public string Password { get; set; } = "";

        public AuthCreditionalsRequest ToRequest() => new() { Login = Login, Password = Password };
    }

    private AuthCreditionalsModel auth = new();

    [Inject] public IAuthenticationService AuthenticationService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public IMessageService messageService { get; set; } = default!;

    [Inject] public HttpClient client { get; set; } = default!;

    [Parameter] public string AfterLoginUrl { get; set; } = "/dev";

    public bool ShowAuthError { get; set; }
    public string? Error;

    private bool IsAlreadyAuth = false;

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

        IsAlreadyAuth = authState.User?.Identity?.IsAuthenticated ?? false;

        if (IsAlreadyAuth)
        {
            NavigationManager.NavigateTo(AfterLoginUrl);
        }

        authVariantConstOption = Q.Site.GetRequiredOption<AuthVariantConstOption>();

        var querystring = HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);

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
                    NavigationManager.NavigateTo(auth2.RedirectUrl, true);
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
        var result = await AuthenticationService.Login(auth.ToRequest());
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
                NavigationManager.NavigateTo(AfterLoginUrl);
            }
            else
            {
                NavigationManager.NavigateTo(ReturnUrl);
            }
        }
    }

    public async void ThirdLogin(string ssoName)
    {
        IsLoginLoading = true;
        ShowAuthError = false;
        StateHasChanged();

        var querystring = HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);
        AuthenticationService authenticationService = (AuthenticationService as AuthenticationService)!;
        var res = await authenticationService.SSOLogin(ssoName, ReturnUrl, NavigationManager.Uri.Split('?')[0], querystring.ToString()!);

        if (res.Action == AuthStepAction.Redirect)
        {
            NavigationManager.NavigateTo(res.RedirectUrl, true);
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
            await authenticationService.Login(res.AuthResultResponse!);

            if (string.IsNullOrEmpty(ReturnUrl) == false)
            {
                NavigationManager.NavigateTo(ReturnUrl);
            }
            else
            {
                NavigationManager.NavigateTo(AfterLoginUrl);
            }
        }
    }

    async void SendThirdAuthDataToServer()
    {
        AuthenticationService authenticationService = (AuthenticationService as AuthenticationService)!;

        if (Data is null) throw new ArgumentNullException(nameof(Data));

        var result = await authenticationService.ThirdLogin(AuthProvider, Data);
        IsLoginLoading = false;

        if (result.Ok)
        {
            _ = messageService.Success("success");
            NavigationManager.NavigateTo(ReturnUrl ?? "/");

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
        var querystring = HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);

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

        var req = await client.SendAsync(request);

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
}
