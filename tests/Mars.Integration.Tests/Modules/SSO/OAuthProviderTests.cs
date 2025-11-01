using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.SSO.Dto;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Options.Models;
using Mars.SSO.Host.OAuth.Controllers;
using Mars.SSO.Host.OAuth.interfaces;
using Mars.SSO.Host.OAuth.Services;
using Mars.Test.Common.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Modules.SSO;

public class OAuthProviderTests : ApplicationTests
{
    const string _apiUrl = "/api/oauth";
    const string _clientAppRedirectUri = "https://localhost.test.app/login";
    const string _requestScopeString = "openid profile email";
    private readonly IOptionService _optionService;
    private IOAuthService _OAuthService;
    const string ClientId = "mars_client";
    const string ClientSecret = "mars_secret";
    const string _profileUrl = "/api/Account/Profile";

    public OAuthProviderTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _optionService = appFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var ssoOpt = _optionService.GetOption<OpenIDServerOption>();
        ssoOpt.OpenIDClientConfigs.Add(new OpenIDServerClientConfig
        {
            Enable = true,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            AllowedGrantTypes = "authorization_code,password,client_credentials",
            CallbackUrl = "",
            RequirePkce = false,
            RedirectUris = "*"
        });
        _optionService.SetOptionOnMemory(ssoOpt);
        _OAuthService = AppFixture.ServiceProvider.GetRequiredService<IOAuthService>();
    }

    [IntegrationFact]
    public async Task AuthorizationUrl_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(OAuthHostController.Authorize);
        _ = nameof(OAuthService.CreateAuthorizationCodeAsync);
        var client = AppFixture.GetClient(true);
        var state = Guid.NewGuid().ToString();
        var query = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["redirect_uri"] = _clientAppRedirectUri,
            ["response_type"] = "code",
            ["state"] = state,
            ["scope"] = _requestScopeString,
        };

        //Act
        var res = await client.Request(_apiUrl, "authorize")
                                .WithSettings(s => s.Redirects.Enabled = false)
                                .AppendQueryParam(query)
                                .AllowAnyHttpStatus()
                                .GetAsync()
                                .CatchUserActionError();

        //Assert
        if (res.StatusCode == StatusCodes.Status400BadRequest)
        {
            var validateErrors = await res.GetJsonAsync<ValidationProblemDetails>();
        }
        res.StatusCode.Should().Be(StatusCodes.Status302Found);
        res.Headers.TryGetFirst("Location", out var ssoProviderRedirectUrl);
        ssoProviderRedirectUrl.Should().NotBeEmpty();
        ssoProviderRedirectUrl.Should().Contain(OAuthPageController.LoginPageUrl);

    }

    [IntegrationFact]
    public async Task ExchangeCodeForToken_Valid_ShouldSuccess()
    {
        //Arrange
        _ = nameof(OAuthHostController.Token);
        _ = nameof(OAuthService.ExchangeCodeForTokenAsync);
        var client = AppFixture.GetClient(true);
        var state = Guid.NewGuid().ToString();
        var scopes = _requestScopeString?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var (code, auth) = await _OAuthService.CreateAuthorizationCodeAsync(ClientId, _clientAppRedirectUri, state, UserConstants.TestUserId, "", "S256", scopes, default);
        var query = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _clientAppRedirectUri,
            ["state"] = state,
            ["scope"] = _requestScopeString,
        };

        //Act
        var res = await client.Request(_apiUrl, "token")
                                .AllowAnyHttpStatus()
                                .PostUrlEncodedAsync(query)
                                .CatchUserActionError();

        //Assert
        if (res.StatusCode == StatusCodes.Status400BadRequest)
        {
            var validateErrors = await res.GetJsonAsync<ValidationProblemDetails>();
        }
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        var response = await res.GetJsonAsync<OpenIdTokenResponse>();
        response.AccessToken.Should().NotBeNullOrEmpty();
        RequestProfileShouldOk(client, response.AccessToken);
    }

    [IntegrationFact]
    public async Task PasswordGrantAsync_GetAccessTokenByPassword_ShouldSuccess()
    {
        //Arrange
        _ = nameof(OAuthHostController.Token);
        _ = nameof(OAuthService.PasswordGrantAsync);
        var client = AppFixture.GetClient(true);

        var scopes = _requestScopeString?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var query = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["grant_type"] = "password",
            //["redirect_uri"] = _clientAppRedirectUri,
            ["scope"] = _requestScopeString,
            ["username"] = UserConstants.TestUserUsername,
            ["password"] = UserConstants.TestUserPassword,
        };

        //Act
        var res = await client.Request(_apiUrl, "token")
                                .AllowAnyHttpStatus()
                                .PostUrlEncodedAsync(query)
                                .CatchUserActionError();

        //Assert
        if (res.StatusCode == StatusCodes.Status400BadRequest)
        {
            var validateErrors = await res.GetJsonAsync<ValidationProblemDetails>();
        }
        res.StatusCode.Should().Be(StatusCodes.Status200OK);
        var response = await res.GetJsonAsync<OpenIdTokenResponse>();
        response.AccessToken.Should().NotBeNullOrEmpty();

        RequestProfileShouldOk(client, response.AccessToken);
    }

    void RequestProfileShouldOk(IFlurlClient client, string accessToken)
        => client.WithOAuthBearerToken(accessToken).Request(_profileUrl).GetAsync().RunSync().StatusCode.Should().Be(StatusCodes.Status200OK);
}
