using Mars.Host.Models;
using Mars.Host.Services;
using Mars.Host.Services.Keycloak;
using Mars.Host.Services.MarsOpenID;
using Mars.Host.Services.MarsSSOClient;
using Mars.Host.Shared.Mappings.Accounts;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Mars.Shared.Contracts.SSO;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class OAuthController : ControllerBase
{
    protected readonly IOptionService _optionService;
    protected readonly MarsSSOOpenIDServerService _marsSSOOpenIDServerService;
    protected readonly MarsSSOClientService _marsSSOClientService;
    protected readonly EsiaService _esiaService;
    protected readonly KeycloakService _keycloakService;

    public OAuthController(IOptionService optionService, MarsSSOOpenIDServerService MarsSSOOpenIDServerService,
        EsiaService esiaService, KeycloakService keycloakService, MarsSSOClientService MarsSSOClientService)
    {
        _optionService = optionService;
        _marsSSOOpenIDServerService = MarsSSOOpenIDServerService;
        _marsSSOClientService = MarsSSOClientService;
        _esiaService = esiaService;
        _keycloakService = keycloakService;
    }

    [HttpPost]
    public async Task<AuthStepsResponse> SSOLogin(
        [FromQuery] string ssoName, [FromQuery] string? returnUrl, [FromQuery] string redirectUrl, [FromQuery]
        string? state, [FromQuery] string? code, CancellationToken cancellationToken)
    {
        var ssoOption = _optionService.GetOption<OpenIDClientOption>().OpenIDClientConfigs.FirstOrDefault(s => s.Slug == ssoName && s.Enable)
            ?? throw new ArgumentNullException("sso config not found");

        if (ssoOption.Driver == "esia")
        {
            if (HttpContext.Request.Query.ContainsKey("data") == false)
            {
                return new AuthStepsResponse
                {
                    Action = AuthStepAction.Redirect,
                    RedirectUrl = ssoOption.AuthEndpoint
                };
            }
            else
            {
                var data = await HttpContext.Request.ReadFromJsonAsync<string[]>();
                return new AuthStepsResponse
                {
                    Action = AuthStepAction.Complete,
                    AuthResultResponse = _esiaService.EsiaLogin(data.ElementAt(0), cancellationToken).ToResponse()
                };
            }
        }
        else
        {
            if (ssoOption.Driver == "keycloak")
            {
                #region KEYCLOAK
                if (state is null)
                {
                    return new AuthStepsResponse
                    {
                        Action = AuthStepAction.Redirect,
                        RedirectUrl = _keycloakService.GenerateRedirectUrl(ssoName, Guid.NewGuid(), redirectUrl, returnUrl)
                    };
                }
                else
                {
                    try
                    {
                        var tokenResponse = await _keycloakService.KeycloakRequestUserJwtByCode(ssoName, code, redirectUrl, cancellationToken);
                        var login = await _keycloakService.KeycloakLogin(tokenResponse, cancellationToken);

                        return new AuthStepsResponse
                        {
                            Action = login.IsAuthSuccessful ? AuthStepAction.Complete : AuthStepAction.Error,
                            AuthResultResponse = login.IsAuthSuccessful ? login.ToResponse() : null,
                            ErrorMessage = login.IsAuthSuccessful ? "" : login.ErrorMessage
                        };
                    }
                    catch (BadHttpRequestException ex)
                    {
                        return new AuthStepsResponse
                        {
                            Action = AuthStepAction.Error,
                            ErrorMessage = ex.Message
                        };
                    }
                }
                #endregion
            }
            else if (ssoOption.Driver == "mars")
            {
                #region MarsSSOremote
                if (state is null)
                {
                    return new AuthStepsResponse
                    {
                        Action = AuthStepAction.Redirect,
                        RedirectUrl = _marsSSOClientService.GenerateRedirectUrl(ssoName, Guid.NewGuid(), redirectUrl, returnUrl)
                    };
                }
                else
                {
                    try
                    {
                        var tokenResponse = await _marsSSOClientService.RequestUserJwtByCode(ssoName, code, redirectUrl, cancellationToken);
                        var login = await _marsSSOClientService.RemoteMarsLogin(tokenResponse, ssoName, cancellationToken);

                        return new AuthStepsResponse
                        {
                            Action = login.IsAuthSuccessful ? AuthStepAction.Complete : AuthStepAction.Error,
                            AuthResultResponse = login.IsAuthSuccessful ? login.ToResponse() : null,
                            ErrorMessage = login.IsAuthSuccessful ? "" : login.ErrorMessage
                        };
                    }
                    catch (BadHttpRequestException ex)
                    {
                        return new AuthStepsResponse
                        {
                            Action = AuthStepAction.Error,
                            ErrorMessage = ex.Message
                        };
                    }
                    catch (Exception ex)
                    {
                        return new AuthStepsResponse
                        {
                            Action = AuthStepAction.Error,
                            ErrorMessage = ex.Message
                        };
                    }
                }
                #endregion
            }
            else
            {
                throw new NotImplementedException($"openID sso '{ssoName}' driver not found");
            }

        }

    }

    /// <summary>
    /// OpenIDLogin
    /// <see cref="AppAdmin.Pages.Public.LoginForm"/>
    /// </summary>
    /// <param name="form"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<AuthStepsResponse> Login([FromForm] OpenIDAuthFormLoginPass form)
    {
        try
        {
            var redirectUri = await _marsSSOOpenIDServerService.Auth(form, HttpContext.Request.Query);

            return new AuthStepsResponse
            {
                Action = AuthStepAction.Redirect,
                RedirectUrl = redirectUri,
            };
        }
        catch (OpenIDAuthException ex)
        {
            return new AuthStepsResponse
            {
                Action = AuthStepAction.Redirect,
                RedirectUrl = _marsSSOOpenIDServerService.ErrorUriResponse(ex, form.RedirectUri),
            };
        }
    }

    //templary for esia
    [HttpPost]
    public async Task<AuthStepsResponse> LoginAccessToken([FromForm] OpenIDAuthFormAccessToken form)
    {
        try
        {
            var redirectUri = await _marsSSOOpenIDServerService.AuthAccessToken(form, HttpContext.Request.Query);

            return new AuthStepsResponse
            {
                Action = AuthStepAction.Redirect,
                RedirectUrl = redirectUri,
            };
        }
        catch (OpenIDAuthException ex)
        {
            return new AuthStepsResponse
            {
                Action = AuthStepAction.Redirect,
                RedirectUrl = _marsSSOOpenIDServerService.ErrorUriResponse(ex, form.RedirectUri),
            };
        }
    }

    /// <summary>
    /// </summary>
    /// <see cref="MarsSSOClientService.RequestUserJwtByCode"/>
    /// <param name="form"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    public OpenIDAuthTokenResponse AuthToken([FromForm] MarsSSOAuthTokenRequest form, CancellationToken cancellationToken)
    {
        var token = _marsSSOOpenIDServerService.Token(form, cancellationToken);

        return token;
    }

    [HttpPost]
    public OpenIdUserInfoResponse UserInfo([FromForm] OpenIDUserInfoRequest form)
    {
        var userInfo = _marsSSOOpenIDServerService.UserInfo(form);
        return userInfo;
    }
}
