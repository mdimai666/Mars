using FluentAssertions;
using Mars.E2E.Tests.Fixtures;
using Mars.E2E.Tests.Helpers;
using Mars.Integration.Tests.Attributes;
using Mars.Test.Common.Constants;
using Microsoft.Playwright;

namespace Mars.E2E.Tests.Tests;

public class UpdateUserProfileTests : BaseE2ETests
{
    public UpdateUserProfileTests(E2EServerFixture appFixture) : base(appFixture)
    {

    }

    [IntegrationFact]
    public async Task UpdateUserProfile_EditData_ShouldSuccess()
    {
        // Arrange
        var tracker = new BrowserErrorTracker(Page);
        await Page.GotoAsync($"{BaseUrl}/dev");

        // Act
        throw new NotImplementedException();

        await Page.WaitForURLAsync("**/dev");

        // Assert
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var userName = await Page.TextContentAsync(".navbar .user-name");
        userName.Should().Contain(UserConstants.TestUserEmail);
        tracker.AssertNoErrors();
    }

}
