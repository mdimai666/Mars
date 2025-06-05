using System.Text;
using System.Text.Json;
using Flurl;
using Flurl.Http;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Models;
using Mars.Host.Services.Keycloak;
using Mars.Host.Services.MarsOpenID;
using Mars.Host.Shared.Dto.Auth;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Services.MarsSSOClient;

public class MarsSSOClientService
{
    readonly IOptionService optionService;
    readonly ITokenService tokenService;
    readonly AccountsService accountsService;
    readonly MarsDbContext ef;
    readonly IUserService userService;
    readonly IMemoryCache memoryCache;

    public MarsSSOClientService(IOptionService optionService, ITokenService tokenService,
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
        var stateDate = new BetweenReqData { RedirectUrl = redirectUrl, State = state, ReturnUrl = returnUrl };

        memoryCache.Set(state.ToString(), stateDate, TimeSpan.FromMinutes(5));

        var ssoOption = optionService.GetOption<OpenIDClientOption>().OpenIDClientConfigs.FirstOrDefault(s => s.Slug == ssoSlug && s.Enable)
            ?? throw new ArgumentNullException("sso config not found");

        string oauth2_auth_endpoint = ssoOption.AuthEndpoint;
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

    static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<OpenIDAuthTokenResponse> RequestUserJwtByCode(string ssoSlug, string code, string redirect_uri, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(ssoSlug, nameof(ssoSlug));
        ArgumentException.ThrowIfNullOrEmpty(code, nameof(code));

        var ssoOption = optionService.GetOption<OpenIDClientOption>().OpenIDClientConfigs.FirstOrDefault(s => s.Slug == ssoSlug && s.Enable)
            ?? throw new ArgumentNullException("sso config not found");

        string client_id = ssoOption.ClientId;
        string client_secret = ssoOption.ClientSecret;
        string oauth2_auth_endpoint = ssoOption.AuthEndpoint;

        var body = new List<KeyValuePair<string, string>>
        {
            new ("client_id", client_id),
            new ("client_secret", client_secret),
            new ("code", code),
            new ("grant_type", "authorization_code"),
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
            return JsonSerializer.Deserialize<OpenIDAuthTokenResponse>(await req.Content.ReadAsStringAsync(cancellationToken), _jsonSerializerOptions)!;
        }
        else
        {
            throw new BadHttpRequestException(req.ReasonPhrase ?? req.StatusCode.ToString());
        }
    }

    public async Task<UserEntity> ConvertUser(MarsJwtTokenUserUnfo userData)
    {
        var user = new UserEntity
        {
            Id = userData.Id,
            Email = userData.Email,
            LockoutEnabled = true,
        };

        var prefererName = await accountsService.FindPrefererUserName(userData.Username);

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

    //public void UpdateUser(ref User user, MarsJwtTokenUserUnfo userData)
    //{
    //    throw new NotImplementedException();
    //}

    //public void UpdateUser(ref UserEntity user, OpenIdUserInfoResponse userData)
    //{
    //    throw new NotImplementedException();
    //}

    public void UpdateUser(ref UserEntity user, MarsJwtTokenUserUnfo userData)
    {
        //dont change user name

        user.FirstName = userData.FirstName;
        user.LastName = userData.LastName;
        user.Email = userData.Email;
        //user.EmailConfirmed = ;
        user.ModifiedAt = DateTime.Now;
    }

    public void UpdateUser(ref UserEntity user, OpenIdUserInfoResponse userInfo)
    {
        //dont change user name

        user.Id = userInfo.Id;
        user.FirstName = userInfo.FirstName;
        user.LastName = userInfo.LastName;
        user.MiddleName = userInfo.MiddleName;
        user.Email = userInfo.Email;
        //user.About = userInfo.About;
        user.PhoneNumber = user.PhoneNumber;
        user.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
        user.Gender = (Mars.Host.Data.OwnedTypes.Users.UserGender)userInfo.Gender;
        user.BirthDate = userInfo.BirthDate ?? DateTime.MinValue;
        //user.AvatarUrl = userInfo.AvatarUrl;

        //user.GeoRegionId = userInfo.GeoRegionId;
        //user.GeoMunicipalityId = userInfo.GeoMunicipalityId;
        //user.GeoLocationId = userInfo.GeoLocationId;

        user.ModifiedAt = DateTimeOffset.Now;
    }

    public async Task<OpenIdUserInfoResponse?> RequserUserInfoFromMarsSite(Guid userId, string ssoSlug)
    {
        var ssoOption = optionService.GetOption<OpenIDClientOption>().OpenIDClientConfigs.FirstOrDefault(s => s.Slug == ssoSlug && s.Enable)
           ?? throw new ArgumentNullException("sso config not found");

        var request = new OpenIDUserInfoRequest() { UserId = userId, ClientId = ssoOption.ClientId, ClientSecret = ssoOption.ClientSecret };

        try
        {
            var host = new Uri(ssoOption.TokenEndpoint).GetLeftPart(UriPartial.Authority);
            var userInfo = await host.ToString()
            .AppendPathSegment("/api/OAuth/UserInfo")
            .PostUrlEncodedAsync(request.ToForm())
            .ReceiveJson<OpenIdUserInfoResponse>();

            return userInfo;
        }
        catch (FlurlHttpException ex)
        {
            Console.Error.WriteLine(ex);
            return null;
        }

    }

    //public async Task<AuthResponseDto> RemoteMarsLogin(string ssoSlug, string data, string redirectUrl, CancellationToken cancellationToken)
    public async Task<AuthResultDto> RemoteMarsLogin(OpenIDAuthTokenResponse tokenResponse, string ssoName, CancellationToken cancellationToken)
    {
        var userInfo = tokenService.JwtDecode<MarsJwtTokenUserUnfo>(tokenResponse.AccessToken, "", verify: false)!;

        if (userInfo is null)
        {
            MarsLogger.GetStaticLogger<MarsSSOClientService>().LogError("token=" + tokenResponse.AccessToken);
        }

        try
        {
            if (userInfo != null)
            {
                var existUser = ef.Users.FirstOrDefault(s => s.Id == userInfo.Id);

                if (existUser is not null)
                {
                    var userFullInfo = await RequserUserInfoFromMarsSite(userInfo.Id, ssoName);
                    if (userFullInfo is not null)
                        UpdateUser(ref existUser, userFullInfo);
                    else
                        UpdateUser(ref existUser, userInfo);

                    ef.SaveChanges();

                    var roles = ef.Roles.Where(s => userInfo.Role.Contains(s.Name)).Select(s => s.Name).ToList();
                    await userService.UpdateUserRoles(existUser.Id, roles!, cancellationToken);

                    return accountsService.LoginForce(existUser.Id).Result;
                }
                else
                {
                    var password = Guid.NewGuid().ToString();

                    var createUser = await ConvertUser(userInfo);
                    var userFullInfo = await RequserUserInfoFromMarsSite(userInfo.Id, ssoName);
                    if (userFullInfo is not null)
                        UpdateUser(ref createUser, userFullInfo);

                    var result = await accountsService.RegisterUser(createUser, password);

                    if (result.IsSuccessfulRegistration)
                    {
                        existUser = ef.Users.FirstOrDefault(s => s.Id == userInfo.Id);
                        var roles = ef.Roles.Where(s => userInfo.Role.Contains(s.Name)).Select(s => s.Name).ToList();
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
            return AuthResultDto.ErrorResponse("Необработанная ошибка bcc08370-152a-4ca5-b71c-c71263a822a6. " + ex.Message);
        }
    }
}
