using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Models;
using Mars.Host.Services.MarsOpenID;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Mars.Host.Services.MarsSSOClient;

public class MarsSSOOpenIDServerService
{
    protected readonly IOptionService optionService;
    protected readonly AccountsService accountsService;
    protected readonly MarsDbContext ef;
    protected readonly IMemoryCache memoryCache;
    protected readonly UserManager<UserEntity> userManager;
    protected readonly IUserService userService;
    readonly ITokenService tokenService;

    public MarsSSOOpenIDServerService(IOptionService optionService, AccountsService accountsService,
        MarsDbContext ef, IMemoryCache memoryCache, UserManager<UserEntity> userManager, IUserService userService, ITokenService tokenService)
    {
        this.optionService = optionService;
        this.accountsService = accountsService;
        this.ef = ef;
        this.memoryCache = memoryCache;
        this.userManager = userManager;
        this.userService = userService;
        this.tokenService = tokenService;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="form"></param>
    /// <returns>redirect url</returns>
    /// <exception cref="OpenIDAuthException"></exception>
    public async Task<string> Auth(OpenIDAuthFormLoginPass form, IQueryCollection requestQuery)
    {
        if (!form.ResponseType.Equals("code")) throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.unsupported_response_type, "ResponseType allow only 'code'");
        if (!form.GrantType.Equals("authorization_code")) throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.invalid_request, "GrantType allow only 'authorization_code'");

        var ssoOption = optionService.GetOption<OpenIDServerOption>().OpenIDClientConfigs.FirstOrDefault(s => s.ClientId == form.ClientId && s.Enable)
            ?? throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.invalid_resource, "sso client config not found");

        var user = await userManager.FindByNameAsync(form.Username);

        if (user == null || !await userManager.CheckPasswordAsync(user, form.Password))
        {
            throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.unauthorized_client, "incorrect user data");
        }

        var (code_key, data) = OpenIDAuthAccessCodeData.Generate(optionService.SysOption.SiteName, form.ClientId, user.Id, form);
        memoryCache.Set(code_key, data, TimeSpan.FromMinutes(5));

        List<string> ignoreKeys = ["redirect_uri"];

        var query = new QueryBuilder((new List<KeyValuePair<string, StringValues>>
        {
            new ("client_id", form.ClientId),
            //new ("redirect_uri", form.RedirectUri),
            //new ("response_type", "code"),
            //new ("state", state.ToString()),
            new ("scope", form.Scope),
            new ("code", code_key),
            //new ("grant_type", "client_credentials"),
            //new ("nonce", nonce.ToString()),
            //new ("sso", form),
        }).Concat(requestQuery.Where(s => !ignoreKeys.Contains(s.Key))).ToList());

        return $"{form.RedirectUri}{query}";

    }

    public string ErrorUriResponse(OpenIDAuthException ex, string redirectUri)
    {
        var query = new QueryBuilder(new List<KeyValuePair<string, string>>
        {
            new ("error", ex.Error),
            new ("error_description", ex.ErrorDescription),
        });

        return $"{redirectUri}{query}";
    }

    public OpenIDAuthTokenResponse Token(MarsSSOAuthTokenRequest form, CancellationToken cancellationToken)
    {
        if (!form.GrantType.Equals("authorization_code")) throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.unsupported_response_type, "GrantType allow only 'authorization_code'");

        CheckClientSecret(form.ClientId, form.ClientSecret);

        if (!memoryCache.TryGetValue<OpenIDAuthAccessCodeData>(form.Code, out var code_state))
        {
            throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.invalid_resource, "code not found or expire");
        }

        var existUser = ef.Users.FirstOrDefault(s => s.Id == code_state.UserId);

        var auth = accountsService.LoginForce(existUser.Id).ConfigureAwait(false).GetAwaiter().GetResult();

        if (auth.IsAuthSuccessful == false)
            throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.login_required, auth.ErrorMessage);

        OpenIDAuthTokenResponse tokenResponse = new()
        {
            AccessToken = auth.Token!,
            TokenType = "Bearer",
            Scope = code_state.Form.Scope,
            RefreshToken = null,
            RefreshExpiresIn = 0,
            ExpiresIn = auth.ExpiresIn,
            SessionState = null,
        };

        return tokenResponse;
    }

    /// <summary>
    /// check client secret
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <exception cref="OpenIDAuthException"></exception>
    void CheckClientSecret(string clientId, string clientSecret)
    {
        var ssoOption = optionService.GetOption<OpenIDServerOption>().OpenIDClientConfigs.FirstOrDefault(s => s.ClientId == clientId && s.Enable)
            ?? throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.unauthorized_client, "sso client config not found");

        if (ssoOption.ClientSecret != clientSecret)
            throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.unauthorized_client, "sso client unauthorized");
    }

    public OpenIdUserInfoResponse UserInfo(Guid userId)
    {
        //var user = ef.Users.FirstOrDefault(u => u.Id == userId)
        //    ?? throw new Exception("user not found");

        //return new OpenIdUserInfoResponse(user.ToDetail());
        throw new DirectoryNotFoundException();
    }

    public OpenIdUserInfoResponse UserInfo(OpenIDUserInfoRequest request)
    {
        CheckClientSecret(request.ClientId, request.ClientSecret);

        return UserInfo(request.UserId);
    }

    //----------------------
    public async Task<string> AuthAccessToken(OpenIDAuthFormAccessToken form, IQueryCollection requestQuery)
    {
        if (!form.ResponseType.Equals("code")) throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.unsupported_response_type, "ResponseType allow only 'code'");
        if (!form.GrantType.Equals("authorization_code")) throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.invalid_request, "GrantType allow only 'authorization_code'");

        var ssoOption = optionService.GetOption<OpenIDServerOption>().OpenIDClientConfigs.FirstOrDefault(s => s.ClientId == form.ClientId && s.Enable)
            ?? throw new OpenIDAuthException(OpenIdErrorCodesForAuthorizationEndpointErrors.invalid_resource, "sso client config not found");

        //TODO: templary. verify user there
        MarsJwtTokenUserUnfo userInfo = tokenService.JwtDecode<MarsJwtTokenUserUnfo>(form.AccessToken, "", verify: false);

        var user = await userManager.FindByIdAsync(userInfo.Id.ToString());

        var (code_key, data) = OpenIDAuthAccessCodeData.Generate(optionService.SysOption.SiteName, form.ClientId, user.Id, form);
        memoryCache.Set(code_key, data, TimeSpan.FromMinutes(5));

        List<string> ignoreKeys = ["redirect_uri"];

        var query = new QueryBuilder((new List<KeyValuePair<string, StringValues>>
        {
            new ("client_id", form.ClientId),
            //new ("redirect_uri", form.RedirectUri),
            //new ("response_type", "code"),
            //new ("state", state.ToString()),
            new ("scope", form.Scope),
            new ("code", code_key),
            //new ("grant_type", "client_credentials"),
            //new ("nonce", nonce.ToString()),
            //new ("sso", form),
        }).Concat(requestQuery.Where(s => !ignoreKeys.Contains(s.Key))).ToList());

        return $"{form.RedirectUri}{query}";

    }
}
