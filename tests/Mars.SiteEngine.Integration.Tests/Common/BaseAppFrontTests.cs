using AutoFixture;
using Flurl.Http;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Mars.Test.Common.FixtureCustomizes;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace Mars.SiteEngine.Integration.Tests.Common;

/// <summary>
/// Фикстура одна на весь сьют (коллекция xUnit): два отдельных IClassFixture-инстанса
/// поднимали два приложения в одном процессе и ломали друг друга через общее статическое состояние.
/// </summary>
public abstract class BaseAppFrontTests<TAppFixture> where TAppFixture : ApplicationFixture
{
    protected readonly TAppFixture AppFixture;
    //protected MarsDbContext DbContext => AppFixture.DbFixture.DbContext;

    private readonly Lazy<IFixture> _fixtureLazy;

    /// <summary>
    /// Фикстура AutoFixture, заранее кастомизированная ссылками на сид-сущности БД
    /// текущей фикстуры (<see cref="ApplicationFixture.Catalog"/>). Создаётся лениво.
    /// </summary>
    public IFixture _fixture => _fixtureLazy.Value;

    protected BaseAppFrontTests(TAppFixture appFixture)
    {
        AppFixture = appFixture;
        AppFixture.DbFixture.Reset().RunSync();
        AppFixture.Seed().RunSync();
        AppFixture.ResetMocks();

        _fixtureLazy = new Lazy<IFixture>(() =>
        {
            var fixture = new Fixture();
            fixture.Customize(new FixtureCustomize(AppFixture.Catalog));
            return fixture;
        });
    }

    public virtual Task<string> RenderRequestPage(string url)
    {
        var client = AppFixture.GetClient();
        return client.Request(url).GetStringAsync();
    }

    public virtual async Task<(string html, int statusCode)> RenderRequestPageEx(string url)
    {
        var client = AppFixture.GetClient();
        var res = await client.Request(url).AllowAnyHttpStatus().GetAsync();
        var html = await res.GetStringAsync();
        return (html, res.StatusCode);
    }
}
