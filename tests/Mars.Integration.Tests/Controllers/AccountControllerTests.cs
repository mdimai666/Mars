using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Shared.Dto.Auth;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Microsoft.AspNetCore.Http;
using static Mars.Test.Common.Constants.UserConstants;

namespace Mars.Integration.Tests.Controllers;

/// <seealso cref="AccountController.Login(AuthCreditionalsDto)"/>
public sealed class AccountControllerTests : ApplicationTests
{
    const string _apiUrl = "/api/Account";

    public AccountControllerTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task Account_LoginValidRequest_ShuldSuccess()
    {
        //Arrange
        _ = nameof(AccountController.Login);
        var client = AppFixture.GetClient(true);
        var request = new AuthCreditionalsDto { Login = TestUserUsername, Password = TestUserPassword };

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment("Login").PostJsonAsync(request).CatchUserActionError().ReceiveJson<AuthResultDto>();

        //Assert
        result.IsAuthSuccessful.Should().BeTrue();
        result.ExpiresIn.Should().BeGreaterThan(0);
        result.Token.Should().NotBeNull();
        result.Token.Length.Should().BeGreaterThan(10);
    }

    [IntegrationFact]
    public async Task Account_LoginInvalidRequest_FailUnauthorizedStatus()
    {
        //Arrange
        _ = nameof(AccountController.Login);
        var client = AppFixture.GetClient(true);
        var request = new AuthCreditionalsDto { Login = TestUserUsername, Password = "invalid_password" };

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment("Login").AllowAnyHttpStatus().PostJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
