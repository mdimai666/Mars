using System.Text;
using System.Text.Json;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Models;
using Mars.Host.Shared.Dto.Auth;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Host.Services.Keycloak;

public partial class KeycloakService
{
    readonly IOptionService optionService;
    readonly ITokenService tokenService;
    readonly AccountsService accountsService;
    readonly MarsDbContext ef;
    readonly IUserService userService;
    readonly IMemoryCache memoryCache;

    public KeycloakService(IOptionService optionService, ITokenService tokenService,
        AccountsService accountsService, MarsDbContext ef, IMemoryCache memoryCache, IUserService userService)
    {
        this.optionService = optionService;
        this.tokenService = tokenService;
        this.accountsService = accountsService;
        this.ef = ef;
        this.memoryCache = memoryCache;
        this.userService = userService;
    }

    public string GenerateRedirectUrl(string ssoSlug, Guid state, string redirectUrl, string returnUrl)
    {
        Guid nonce = Guid.NewGuid();
        var stateData = new BetweenReqData { RedirectUrl = redirectUrl, State = state, ReturnUrl = returnUrl };

        memoryCache.Set(state.ToString(), stateData, TimeSpan.FromMinutes(5));

        var ssoOption = optionService.GetOption<OpenIDClientOption>().OpenIDClientConfigs.FirstOrDefault(s => s.Slug == ssoSlug && s.Enable)
            ?? throw new ArgumentNullException("sso config not found");
        string oauth2_auth_endpoint = ssoOption.AuthEndpoint; // http://localhost:6767/realms/{realm}/protocol/openid-connect/auth
        string client_id = ssoOption.ClientId;
        var query = new QueryBuilder(new List<KeyValuePair<string, string>>
        {
            new ("client_id", client_id),
            new ("redirect_uri", redirectUrl),
            new ("returnUrl", returnUrl??""),
            new ("response_type", "code"),
            new ("state", state.ToString()),
            new ("scope", ssoOption.Scopes),
            new ("grant_type", "client_credentials"),
            new ("nonce", nonce.ToString()),
            new ("sso", ssoSlug),
        });

        return $"{oauth2_auth_endpoint}{query}";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code">после авторизации через интерфейс отдается code</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="BadHttpRequestException"></exception>
    public async Task<OpenIDAuthTokenResponse> KeycloakRequestUserJwtByCode(string ssoSlug, string code, string redirect_uri, CancellationToken cancellationToken)
    {
        var ssoOption = optionService.GetOption<OpenIDClientOption>().OpenIDClientConfigs.FirstOrDefault(s => s.Slug == ssoSlug && s.Enable)
            ?? throw new ArgumentNullException("sso config not found");

        string client_id = ssoOption.ClientId;
        string client_secret = ssoOption.ClientSecret;
        string oauth2_auth_endpoint = ssoOption.AuthEndpoint;

        //string tokenUrl = @$"http://localhost:6767/realms/{realm}/protocol/openid-connect/token";

        var body = new List<KeyValuePair<string, string>>
        {
            new ("client_id", client_id),
            new ("client_secret", client_secret),
            new ("code", code),
            new ("grant_type", "authorization_code"),
            //new ("grant_type", "password"),
            //new ("username", username),
            //new ("password", "user"),
            new ("redirect_uri", redirect_uri),
        };

        HttpClient client = new HttpClient();

        var form = new FormUrlEncodedContent(body).ReadAsStringAsync().Result;
        HttpContent post = new StringContent(form, Encoding.UTF8, "application/x-www-form-urlencoded");
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ssoOption.TokenEndpoint);
        request.Content = post;

        var req = await client.SendAsync(request, cancellationToken);
        if (req.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<OpenIDAuthTokenResponse>(await req.Content.ReadAsStringAsync(cancellationToken))!;
        }
        else
        {
            throw new BadHttpRequestException(req.ReasonPhrase ?? req.StatusCode.ToString());
        }
    }

#if false
    string example_json = """
                {
          "exp": 1708258045,
          "iat": 1708257745,
          "auth_time": 1708257551,
          "jti": "4075b9e2-3304-44f4-ad9c-0b54621ea575",
          "iss": "http://localhost:6767/realms/myrealm",
          "aud": "account",
          "sub": "762cf476-1512-40ba-99bf-105bd8067c6c",
          "typ": "Bearer",
          "azp": "myclient",
          "nonce": "2d749f4f-94ba-4310-8510-edcaa6c9f9ec",
          "session_state": "6cc6ee75-d7d1-4400-b337-745363a6928a",
          "acr": "0",
          "allowed-origins": [
            "http://localhost:5003"
          ],
          "realm_access": {
            "roles": [
              "default-roles-myrealm",
              "offline_access",
              "uma_authorization",
              "Admin"
            ]
          },
          "resource_access": {
            "account": {
              "roles": [
                "manage-account",
                "manage-account-links",
                "view-profile"
              ]
            }
          },
          "scope": "openid email profile",
          "sid": "6cc6ee75-d7d1-4400-b337-745363a6928a",
          "email_verified": true,
          "name": "User name Macconohi",
          "preferred_username": "user",
          "given_name": "User name",
          "family_name": "Macconohi",
          "email": "user-non-work@mail.ru"
        }
        """; 
#endif

    public async Task<UserEntity> ConvertUser(KeycloakUserInfo userData)
    {
        var user = new UserEntity
        {
            Id = userData.Id,
            Email = userData.Email,
            LockoutEnabled = true,
        };

        var prefererName = await accountsService.FindPrefererUserName(userData.PreferredUsername);

        if (prefererName is null)
        {
            user.UserName = userData.Email;
        }
        else
        {
            user.UserName = prefererName;
        }

        UpdateUser(ref user, userData);

        return user;
    }

    public void UpdateUser(ref UserEntity user, KeycloakUserInfo userData)
    {
        user.FirstName = userData.Firstname;
        user.LastName = userData.Lastname;
        user.Email = userData.Email;
        user.EmailConfirmed = userData.EmailVerified;
        user.ModifiedAt = DateTime.Now;
    }

    public async Task<AuthResultDto> KeycloakLogin(OpenIDAuthTokenResponse tokenResponse, CancellationToken cancellationToken)
    {
        KeycloakUserInfo userInfo = tokenService.JwtDecode<KeycloakUserInfo>(tokenResponse.AccessToken, verify: false);

        try
        {
            if (userInfo != null)
            {
                var existUser = ef.Users.FirstOrDefault(s => s.Id == userInfo.Id);

                if (existUser != null)
                {
                    UpdateUser(ref existUser, userInfo);
                    ef.SaveChanges();

                    var roles = ef.Roles.Where(s => userInfo.RealmAccess.Roles.Contains(s.Name)).Select(s => s.Name).ToList();
                    await userService.UpdateUserRoles(existUser.Id, roles!, cancellationToken);

                    return accountsService.LoginForce(existUser.Id).Result;
                }
                else
                {
                    var password = Guid.NewGuid().ToString();

                    var user = await ConvertUser(userInfo);

                    var result = await accountsService.RegisterUser(user, password, cancellationToken);

                    if (result.IsSuccessfulRegistration)
                    {
                        existUser = ef.Users.FirstOrDefault(s => s.Id == userInfo.Id);
                        var roles = ef.Roles.Where(s => userInfo.RealmAccess.Roles.Contains(s.Name)).Select(s => s.Name).ToList();
                        await userService.UpdateUserRoles(existUser.Id, roles!, cancellationToken);
                        return accountsService.LoginForce(existUser.Id).Result;
                    }
                    else
                    {
                        return AuthResultDto.ErrorResponse(string.Join(";\n", result.Errors));
                    }

                }
            }
            else
            {
                return AuthResultDto.ErrorResponse("cannot parse token");
            }

        }
        catch (Exception ex)
        {
            return AuthResultDto.ErrorResponse("Необработанная ошибка 945cccfc-5bc8-41c8-a7b3-f9eb61049a83. " + ex.Message);
        }
    }

}
