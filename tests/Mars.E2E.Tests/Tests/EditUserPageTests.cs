using FluentAssertions;
using Flurl.Http;
using Mars.E2E.Tests.Fixtures;
using Mars.E2E.Tests.Helpers;
using Mars.Identity.Contracts.Users;
using Mars.Integration.Tests.Attributes;
using Mars.Test.Common.Constants;
using Microsoft.Playwright;

namespace Mars.E2E.Tests.Tests;

public class EditUserPageTests : BaseE2ETests
{
    public EditUserPageTests(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact(Skip = SkipE2ETests)]
    public async Task EditUserPage_UpdateFields_ShouldPersist()
    {
        // Arrange
        var tracker = new BrowserErrorTracker(Page);
        var userId = UserConstants.TestUserId;
        var newFirstName = "UpdatedFirstName";
        var newLastName = "UpdatedLastName";
        var newMiddleName = "UpdatedMiddleName";
        var newPhoneNumber = "+79161234567"; // Valid international phone number

        await Page.GotoAsync($"{BaseUrl}/dev/EditUser/{userId}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForSelectorAsync("[name='FirstName']", new() { Timeout = 5000 });

        // Act — fill form fields
        // FluentTextField is a web component with input in shadow DOM
        // Use Click + Ctrl+A + PressSequentially to replace existing text
        await FillTextField(Page, "FirstName", newFirstName);
        await Task.Delay(500);
        await FillTextField(Page, "LastName", newLastName);
        await Task.Delay(500);
        await FillTextField(Page, "MiddleName", newMiddleName);
        await Task.Delay(500);
        await FillTextField(Page, "PhoneNumber", newPhoneNumber);
        await Task.Delay(500);

        // Save form and wait for API response
        var saveResponse = await Page.RunAndWaitForResponseAsync(
            async () => await Page.Locator("button[type='submit']").ClickAsync(),
            response => response.Url.Contains("/api/User") && response.Request.Method == "PUT",
            new() { Timeout = 10000 });

        // Verify API response is successful
        saveResponse.Should().NotBeNull("Save API call should be made");
        saveResponse!.Status.Should().Be(200, $"Save should succeed, but got: {await saveResponse.TextAsync()}");

        // Wait for all network requests to complete
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert — verify data updated via API
        var client = AppFixture.GetClient(isAnonymous: false);
        var updatedUser = await client.Request($"api/User", userId).GetJsonAsync<UserDetailResponse?>();

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(newFirstName);
        updatedUser.LastName.Should().Be(newLastName);
        updatedUser.MiddleName.Should().Be(newMiddleName);
        updatedUser.PhoneNumber.Should().Be(newPhoneNumber);

        tracker.AssertNoErrors();
    }

    /// <summary>
    /// Fills a Fluent UI text field by clicking, selecting all, and typing new value.
    /// </summary>
    private static async Task FillTextField(IPage page, string fieldName, string value)
    {
        await page.Locator($"[name='{fieldName}']").ClickAsync();
        await page.Keyboard.PressAsync("Control+a");
        await page.Locator($"[name='{fieldName}']").PressSequentiallyAsync(value, new() { Delay = 10 });
    }
}
