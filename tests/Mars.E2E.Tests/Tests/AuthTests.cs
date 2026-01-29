using FluentAssertions;
using Mars.E2E.Tests.Fixtures;
using Mars.E2E.Tests.Helpers;
using Mars.Integration.Tests.Attributes;
using Mars.Test.Common.Constants;
using Microsoft.Playwright;

namespace Mars.E2E.Tests.Tests;

public class AuthTests : BaseE2ETests
{
    public override bool AuthorizedStart => false;

    public AuthTests(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task LoginPage_ValidData_ShouldSuccess()
    {
        // Arrange
        var tracker = new BrowserErrorTracker(Page);
        await Page.GotoAsync($"{BaseUrl}/dev/Login");

        // Act
        await Page.FillAsync("[name='login-email'] input", UserConstants.TestUserUsername);
        await Page.FillAsync("[name='password'] input", UserConstants.TestUserPassword);
        await Page.ClickAsync("[type='submit'] button");
        await Page.WaitForURLAsync("**/dev");

        // Assert
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var userName = await Page.TextContentAsync(".navbar .user-name");
        userName.Should().Contain(UserConstants.TestUserEmail);
        tracker.AssertNoErrors();
    }
}
