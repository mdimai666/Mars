using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Profile;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.Constants;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Users;

public class GetUserProfileTests : ApplicationTests
{
    const string _apiUrl = "/api/Account";

    public GetUserProfileTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetUserProfile_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(AccountController.Profile);
        _ = nameof(AccountsService.GetProfile);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "Profile").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task GetUserProfile_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(AccountController.Profile);
        _ = nameof(AccountsService.GetProfile);
        var client = AppFixture.GetClient();

        //Act
        var result = await client.Request(_apiUrl, "Profile").GetJsonAsync<UserProfileDto>();

        //Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be(UserConstants.TestUserUsername);
    }
}
