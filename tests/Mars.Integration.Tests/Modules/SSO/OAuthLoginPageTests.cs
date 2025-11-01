using System.Web;
using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Options.Models;
using Mars.SSO.Host.OAuth.Controllers;
using Mars.SSO.Host.OAuth.interfaces;
using Mars.Test.Common.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Modules.SSO;

public class OAuthLoginPageTests : ApplicationTests
{
    const string _apiUrl = "/api/oauth";
    const string _clientAppRedirectUri = "https://localhost.test.app/login";
    const string _requestScopeString = "openid profile email";
    private readonly IOptionService _optionService;
    private IOAuthService _OAuthService;
    const string ClientId = "mars_client";
    const string ClientSecret = "mars_secret";
    const string _profileUrl = "/api/Account/Profile";

    public OAuthLoginPageTests(ApplicationFixture appFixture) : base(appFixture)
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
        _ = nameof(OAuthPageController.LoginPage);
        var client = AppFixture.GetClient(true);
        var state = Guid.NewGuid().ToString();
        var authUrlQuery = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["redirect_uri"] = _clientAppRedirectUri,
            ["response_type"] = "code",
            ["state"] = state,
            ["scope"] = _requestScopeString,
        };

        //Act
        // Step 1: Request Authorization URL
        var authUrlRes = await client.Request(_apiUrl, "authorize")
                                .WithSettings(s => s.Redirects.Enabled = false)
                                .AppendQueryParam(authUrlQuery)
                                .AllowAnyHttpStatus()
                                .GetAsync()
                                .CatchUserActionError();
        authUrlRes.StatusCode.Should().Be(StatusCodes.Status302Found);
        authUrlRes.Headers.TryGetFirst("Location", out var oauthProviderLoginPage);
        oauthProviderLoginPage.Should().NotBeNullOrEmpty();

        /// Step 2: Submit Login Form
        var authUrlQueryDict = HttpUtility.ParseQueryString(new Uri(oauthProviderLoginPage, UriKind.RelativeOrAbsolute).Query);
        var credentialId = authUrlQueryDict["credentialId"]!;
        var loginFormPostData = new Dictionary<string, string>
        {
            ["credential_id"] = credentialId,
            ["username"] = UserConstants.TestUserUsername,
            ["password"] = UserConstants.TestUserPassword,
        };

        var loginRes = await client.Request(oauthProviderLoginPage)
                                .WithSettings(s => s.Redirects.Enabled = false)
                                .PostUrlEncodedAsync(loginFormPostData)
                                .CatchUserActionError();
        loginRes.StatusCode.Should().Be(StatusCodes.Status302Found);
        loginRes.Headers.TryGetFirst("Location", out var callbackCodeResponseUrl);

        // Assert
        callbackCodeResponseUrl.Should().Contain(_clientAppRedirectUri);
        var callbackCodeResponseDict = HttpUtility.ParseQueryString(new Uri(callbackCodeResponseUrl, UriKind.RelativeOrAbsolute).Query);

        string[] checkValues = ["code", "state", "client_id", "redirect_uri"];
        foreach (var val in checkValues)
        {
            callbackCodeResponseDict[val].Should().NotBeNullOrEmpty($"Querystring should contain {val}");
        }

    }
}
