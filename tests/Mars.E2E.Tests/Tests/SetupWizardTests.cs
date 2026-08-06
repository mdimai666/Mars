using FluentAssertions;
using Mars.E2E.Tests.Fixtures;
using Mars.Integration.Tests.Attributes;
using Mars.Test.Common.Constants;
using Microsoft.Playwright;

namespace Mars.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the Setup Wizard.
/// Tests verify: redirect behavior, DB validation, and full wizard flow.
/// </summary>
public class SetupWizardTests : BaseE2ETests
{
    // Override to skip authorization — wizard doesn't require auth
    public override bool AuthorizedStart => false;

    public SetupWizardTests(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact(Skip = SkipE2ETests)]
    public async Task SetupWizard_DatabasePage_InvalidConnection_ShouldShowError()
    {
        // Arrange
        await Page.GotoAsync($"{BaseUrl}/setup/database");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act — fill with invalid credentials and click "Далее"
        await Page.FillAsync("input[name='Host']", "invalid-host-that-does-not-exist");
        await Page.FillAsync("input[name='Port']", "5432");
        await Page.FillAsync("input[name='Database']", "mars");
        await Page.FillAsync("input[name='Username']", "mars");
        await Page.FillAsync("input[name='Password']", "wrongpassword");

        await Page.ClickAsync("button:has-text('Далее')");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert — should show error (auto-validation on "Далее")
        await Page.WaitForSelectorAsync(".alert-danger", new() { Timeout = 15000 });
    }

    [IntegrationFact(Skip = SkipE2ETests)]
    public async Task SetupWizard_DatabasePage_TestConnection_ShouldShowError()
    {
        // Arrange
        await Page.GotoAsync($"{BaseUrl}/setup/database");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act — fill with invalid credentials and click "Проверить подключение"
        await Page.FillAsync("input[name='Host']", "invalid-host-that-does-not-exist");
        await Page.FillAsync("input[name='Port']", "5432");
        await Page.FillAsync("input[name='Database']", "mars");
        await Page.FillAsync("input[name='Username']", "mars");
        await Page.FillAsync("input[name='Password']", "wrongpassword");

        await Page.ClickAsync("button:has-text('Проверить подключение')");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert — should show error
        await Page.WaitForSelectorAsync(".alert-danger", new() { Timeout = 15000 });
        var content = await Page.ContentAsync();
        content.Should().Contain("Ошибка подключения");
    }

    [IntegrationFact(Skip = SkipE2ETests)]
    public async Task SetupWizard_FullFlow_ShouldReachCompletePage()
    {
        // Parse connection string from test fixture
        var connStr = AppFixture.DbFixture.ConnectionString;
        var dbConfig = ParseConnectionString(connStr);

        // Step 1: Database page — fill with valid test DB credentials
        await Page.GotoAsync($"{BaseUrl}/setup/database");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.FillAsync("input[name='Host']", dbConfig.Host);
        await Page.FillAsync("input[name='Port']", dbConfig.Port);
        await Page.FillAsync("input[name='Database']", dbConfig.Database);
        await Page.FillAsync("input[name='Username']", dbConfig.Username);
        await Page.FillAsync("input[name='Password']", dbConfig.Password);

        // Click "Далее" — should pass validation and redirect to user page
        await Page.ClickAsync("button:has-text('Далее')");
        await Page.WaitForURLAsync("**/setup/user", new() { Timeout = 15000 });

        // Step 2: User page — fill admin credentials
        await Page.FillAsync("input[name='FirstName']", "TestAdmin");
        await Page.FillAsync("input[name='Email']", "testadmin@example.com");
        await Page.FillAsync("input[name='Password']", "TestPassword123!");

        // Click "Завершить установку"
        await Page.ClickAsync("button:has-text('Завершить установку')");
        await Page.WaitForURLAsync("**/setup/complete", new() { Timeout = 15000 });

        // Step 3: Complete page — verify it shows
        var content = await Page.ContentAsync();
        content.Should().Contain("Установка завершена");

        // Step 4: Navigate to Login page
        await Page.GotoAsync($"{BaseUrl}/dev/Login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 5: Authorize with test user (already seeded in test DB)
        await Page.FillAsync("[name='login-email'] input", UserConstants.TestUserUsername);
        await Page.FillAsync("[name='password'] input", UserConstants.TestUserPassword);
        await Page.ClickAsync("[type='submit'] button");
        await Page.WaitForURLAsync("**/dev", new() { Timeout = 15000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 6: Navigate to Users page
        await Page.GotoAsync($"{BaseUrl}/dev/Users");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert — Users page should load with the grid
        await Page.WaitForSelectorAsync(".adaptive-table", new() { Timeout = 15000 });
        var usersContent = await Page.ContentAsync();
        usersContent.Should().Contain("testuser@mail.localhost");
    }

    private static (string Host, string Port, string Database, string Username, string Password) ParseConnectionString(string connStr)
    {
        var parts = connStr.Split(';')
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim().ToLower(), p => p[1].Trim());

        return (
            Host: parts.GetValueOrDefault("host", "localhost"),
            Port: parts.GetValueOrDefault("port", "5432"),
            Database: parts.GetValueOrDefault("database", "mars"),
            Username: parts.GetValueOrDefault("username", "mars"),
            Password: parts.GetValueOrDefault("password", "mars")
        );
    }
}
