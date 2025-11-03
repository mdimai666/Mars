using System.Web;
using AppAdmin.Pages.Public;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.SSO.Dto;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Options.Models;
using Mars.Shared.Contracts.NavMenus;
using Mars.Shared.Contracts.SSO;
using Mars.Shared.Contracts.Users.UserProfiles;
using Mars.SSO.Controllers;
using Mars.SSO.Middlewares;
using Mars.SSO.Providers;
using Mars.SSO.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ExternalServices.Integration.Tests.KeycloakTests;

public class KeycloakSSOClientTests : KeycloakIntegrationTestBase
{
    const string _apiUrl = "/api/sso";
    private readonly IOptionService _optionService;

    public KeycloakSSOClientTests(ApplicationFixture appFixture, KeycloakTestContainerFixture keycloak) : base(appFixture, keycloak)
    {
        _optionService = appFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var ssoOpt = _optionService.GetOption<OpenIDClientOption>();
        ssoOpt.OpenIDClientConfigs.Add(new OpenIDClientConfig
        {
            Title = "Keycloak",
            Slug = "keycloak1",
            Enable = true,
            Driver = "keycloak",
            AuthEndpoint = keycloak.AuthEndpoint,
            TokenEndpoint = keycloak.TokenEndpoint,
            ClientId = KeycloakTestContainerFixture.ClientId,
            ClientSecret = KeycloakTestContainerFixture.ClientSecret,
            CallbackPath = "/signin-oidc",
            Scopes = "openid email profile",
            Issuer = keycloak.Issuer,
        });
        _optionService.SetOptionOnMemory(ssoOpt);
    }

    async Task<string> RequestSSORedirectUrl(IFlurlClient marsClient)
    {
        var requestQuery = new { redirectUri = marsClient.BaseUrl };

        //Act - retrive jump Link
        var res = await marsClient.Request(_apiUrl, "login", "keycloak1")
                                .WithSettings(s => s.Redirects.Enabled = false)
                                .AppendQueryParam(requestQuery)
                                .AllowAnyHttpStatus()
                                .GetAsync();
        if (res.StatusCode == StatusCodes.Status400BadRequest)
        {
            var validateErrors = await res.GetJsonAsync<ValidationProblemDetails>();
        }

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status302Found);
        var ssoProviderRedirectUrl = res.GetStringAsync().RunSync()!;
        res.Headers.TryGetFirst("Location", out ssoProviderRedirectUrl);

        // RedirectUrl example
        // http://localhost:6767/realms/myrealm/protocol/openid-connect/auth?client_id=myclient&response_type=code&scope=openid%20profile%20email&redirect_uri=http%3A%2F%2Flocalhost%3A5003%2Fapi%2Findex.html&state=582472eacf0d41b999351baeff23dee3

        //var querystring = HttpUtility.ParseQueryString(new Uri(ssoProviderRedirectUrl).Query);
        //var qDict = querystring.AllKeys.ToDictionary(x => x!, x => querystring[x]);

        return ssoProviderRedirectUrl;
    }

    async Task<OAuthTokenResponse> KeycloackAccessToken(IFlurlClient extClient, string? username = null, string? password = null)
    {
        var keycloakResponse = await extClient.Request(_keycloak.TokenEndpoint)
            .PostUrlEncodedAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = KeycloakTestContainerFixture.ClientId,
                ["client_secret"] = KeycloakTestContainerFixture.ClientSecret,
                ["username"] = username ?? KeycloakTestContainerFixture.Username,
                ["password"] = password ?? KeycloakTestContainerFixture.Password,
            });

        //var keycloakResponseSt = await keycloakResponse.GetStringAsync();
        var keycloakResponseJson = await keycloakResponse.GetJsonAsync<OAuthTokenResponse>();
        //var ts = AppFixture.ServiceProvider.GetRequiredService<ITokenService>();
        //var claims = ts.JwtDecode<KeycloakUserInfo>(keycloakResponseJson.AccessToken, verify: false);
        return keycloakResponseJson;
    }

    [IntegrationFact]
    public async Task ListProviders_List_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SsoController.ListProviders);
        _ = nameof(SsoService.Providers);
        var marsClient = AppFixture.GetClient(true);

        //Act
        var result = await marsClient.Request(_apiUrl, "providers").GetJsonAsync<SsoProviderItemResponse[]>();

        //Assert
        result.Should().HaveCount(1);
        var first = result.First();
        first.Name.Should().Be("keycloak1");
        first.DisplayName.Should().NotBeNullOrEmpty();
    }

    [IntegrationFact]
    public async Task Login_RequestRedirectUrl_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SsoController.Login);
        _ = nameof(KeycloakProvider.GetAuthorizationUrl);
        _ = nameof(LoginForm.ExecuteLogin);
        var marsClient = AppFixture.GetClient(true);
        var requestQuery = new { redirectUri = marsClient.BaseUrl };

        //Act
        var res = await marsClient.Request(_apiUrl, "login", "keycloak1")
                                .WithSettings(s => s.Redirects.Enabled = false)
                                .AppendQueryParam(requestQuery)
                                .AllowAnyHttpStatus()
                                .GetAsync();

        //Assert
        if (res.StatusCode == StatusCodes.Status400BadRequest)
        {
            var validateErrors = await res.GetJsonAsync<ValidationProblemDetails>();
        }

        res.StatusCode.Should().Be(StatusCodes.Status302Found);
        res.Headers.TryGetFirst("Location", out var ssoProviderRedirectUrl);
        ssoProviderRedirectUrl.Should().NotBeNullOrEmpty();
        ssoProviderRedirectUrl.Should().Contain(_keycloak.BaseUrl);
    }

    [IntegrationFact(Skip = "не требуется")]
    public async Task Callback_UserInfo_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SsoController.Login);
        _ = nameof(SsoController.Callback);
        _ = nameof(LoginForm.ExecuteLogin);
        var marsClient = AppFixture.GetClient(true);
        var extClient = new FlurlClient();

        var ssoProviderRedirectUrl = await RequestSSORedirectUrl(marsClient);

        var querystring = HttpUtility.ParseQueryString(new Uri(ssoProviderRedirectUrl).Query);
        var qDict = querystring.AllKeys.ToDictionary(x => x!, x => querystring[x]);
        var requestQuery = new { code = qDict["code"], redirectUri = qDict["redirect_uri"] };

        var res = await marsClient.Request(_apiUrl, "callback", "keycloak1")
                                .WithSettings(s => s.Redirects.Enabled = false)
                                .AppendQueryParam(requestQuery)
                                .AllowAnyHttpStatus()
                                .GetJsonAsync<SsoUserInfoResponse>();
        var a = res;

        //var keycloakResponse = await KeycloackAccessToken(extClient);

        //// try do Authorized request
        //_ = nameof(AccountController.Profile);
        //var profileUrl = "/api/Account/Profile";
        //var profileRes = await marsClient.WithOAuthBearerToken(keycloakResponse.AccessToken).Request(profileUrl).GetAsync();

        //profileRes.StatusCode.Should().Be(StatusCodes.Status200OK);
        //var profileResJson = profileRes.GetJsonAsync<UserProfileResponse>().RunSync();
        //profileResJson.Should().NotBeNull();

        //// UserName может конфликтовать с уже существующими и назначен другой.
        ////profileResJson.UserName.ToString().Should().Be(KeycloakTestContainerFixture.Username);
        //profileResJson.Email.Should().Be(KeycloakTestContainerFixture.UserEmail);

        //profileResJson.Roles.Should().Contain("Admin");

        ////check user exist
    }

    [IntegrationFact]
    public async Task SignUp_UsingKeycloackToken_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SsoController.Login);
        _ = nameof(SsoAuthMiddleware);
        var marsClient = AppFixture.GetClient(true);
        var extClient = new FlurlClient();
        var keycloakResponse = await KeycloackAccessToken(extClient);
        var profileUrl = "/api/Account/Profile";

        //Act
        var profileRes = await marsClient.WithOAuthBearerToken(keycloakResponse.AccessToken).Request(profileUrl).GetAsync();

        //Assert
        profileRes.StatusCode.Should().Be(StatusCodes.Status200OK);
        var profile = await profileRes.GetJsonAsync<UserProfileResponse>();
        // UserName может конфликтовать с уже существующими и назначен другой.
        //profileResJson.UserName.ToString().Should().Be(KeycloakTestContainerFixture.Username);
        profile.Email.Should().Be(KeycloakTestContainerFixture.UserEmail);
        profile.Roles.Should().Contain("Admin");
    }

    [IntegrationFact]
    public async Task SignUp_InvalidSignToken_Should401()
    {
        //Arrange
        _ = nameof(SsoController.Login);
        _ = nameof(SsoAuthMiddleware);
        var marsClient = AppFixture.GetClient(true);
        var extClient = new FlurlClient();
        var keycloakResponse = await KeycloackAccessToken(extClient);
        var profileUrl = "/api/Account/Profile";

        //Act
        var profileRes = await marsClient.WithOAuthBearerToken(keycloakResponse.AccessToken + "invalidTailData")
                                            .AllowAnyHttpStatus()
                                            .Request(profileUrl).GetAsync();

        //Assert
        profileRes.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task SignUp_AdminEndpointRequest_ShouldWork()
    {
        //Arrange
        _ = nameof(SsoController.Login);
        _ = nameof(SsoAuthMiddleware);
        _ = nameof(NavMenuController.List);
        var marsClient = AppFixture.GetClient(true);
        var extClient = new FlurlClient();
        var keycloakResponse = await KeycloackAccessToken(extClient);
        var navMenuListUrl = "/api/NavMenu";
        var request = new ListNavMenuQueryRequest();

        //Act
        var profileRes = await marsClient.WithOAuthBearerToken(keycloakResponse.AccessToken)
                                            .AllowAnyHttpStatus()
                                            .Request(navMenuListUrl).GetAsync();

        //Assert
        profileRes.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
