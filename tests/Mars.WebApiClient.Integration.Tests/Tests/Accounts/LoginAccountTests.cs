using FluentAssertions;
using Mars.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Auth;
using Mars.Test.Common.FixtureCustomizes;
using static Mars.Test.Common.Constants.UserConstants;

namespace Mars.WebApiClient.Integration.Tests.Tests.Accounts;

public class LoginAccountTests : BaseWebApiClientTests
{
    public LoginAccountTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task Login_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(AccountController.Login);
        var client = GetWebApiClient(isAnonymous: true);
        var request = new AuthCreditionalsRequest { Login = TestUserUsername, Password = TestUserPassword };

        //Act
        var result = await client.Account.Login(request);

        //Assert
        result.IsAuthSuccessful.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.ExpiresIn.Should().BeGreaterThan(0);
    }

    [IntegrationFact]
    public async Task Login_InvalidCreditional_Fail()
    {
        //Arrange
        _ = nameof(AccountController.Login);
        var client = GetWebApiClient(isAnonymous: true);
        var request = new AuthCreditionalsRequest { Login = TestUserUsername, Password = "invalid_password" };

        //Act
        var result = await client.Account.Login(request);

        //Assert
        result.IsAuthSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Token.Should().BeNullOrEmpty();
        result.ExpiresIn.Should().Be(0);
    }
}
