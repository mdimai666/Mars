using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.E2E.Tests.Fixtures;
using Mars.Integration.Tests.Extensions;
using Mars.Shared.Contracts.Auth;
using Mars.Test.Common.Constants;
using Microsoft.Playwright;

namespace Mars.E2E.Tests;

[CollectionDefinition("E2ETestApp")]
public class BaseE2ETestsAppCollection : ICollectionFixture<E2EServerFixture>
{
}

[Collection("E2ETestApp")]
public class BaseE2ETests : IAsyncLifetime
{
    public const string? SkipE2ETests = "Skip";

    protected readonly E2EServerFixture AppFixture;
    public IFixture _fixture = new Fixture();

    protected IPlaywright Playwright = null!;
    protected IBrowser Browser = null!;
    protected IBrowserContext Context = null!;
    protected IPage Page = null!;

    private FlurlCookie? _cookie;

    public string BaseUrl => AppFixture.BaseUrl;

    public virtual bool AuthorizedStart { get; } = true;

    public BaseE2ETests(E2EServerFixture appFixture)
    {
        AppFixture = appFixture;
        AppFixture.DbFixture.Reset().RunSync();
        AppFixture.Seed().RunSync();
        AppFixture.ResetMocks();

        // Из-за способа хранения и округления DateTime, оно может на миллисекунды отличаться
        AssertionOptions.AssertEquivalencyUsing(
            options => options
                .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(50))).WhenTypeIs<DateTime>()
                .Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(50))).WhenTypeIs<DateTimeOffset>()
        );
    }

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge", // 🔥 системный Edge из Windows 11
            Headless = false,
            SlowMo = 50
        });

        Context = await Browser.NewContextAsync();
        Page = await Context.NewPageAsync();

        if (AuthorizedStart)
            await AuthorizeAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.CloseAsync();
        await Browser.CloseAsync();
        Playwright.Dispose();
    }

    public async Task AuthorizeAsync()
    {
        await Context.AddInitScriptAsync($@"
            localStorage.setItem('authToken', '{AppFixture.BearerTokenValue}');
        ");

        if (_cookie is null)
        {
            var response = await AppFixture.GetClient(true).Request("/api/Account/Login")
                                        .PostJsonAsync(new AuthCreditionalsRequest
                                        {
                                            Login = UserConstants.TestUserUsername,
                                            Password = UserConstants.TestUserPassword
                                        });
            _cookie = response.Cookies[0];
        }
        await Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = _cookie.Name,
                Value = _cookie.Value,
                //Url = BaseUrl,
                Domain = _cookie.Domain ?? "localhost",
                Path = _cookie.Path,
                HttpOnly = _cookie.HttpOnly,
                Secure = _cookie.Secure,
                SameSite = SameSiteAttribute.Lax,
                Expires = _cookie.Expires!.Value.ToUnixTimeSeconds(),
            }
        ]);
    }
}
