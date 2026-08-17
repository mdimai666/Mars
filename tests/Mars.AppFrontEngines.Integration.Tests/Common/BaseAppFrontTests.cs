using AutoFixture;
using Flurl.Http;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Mars.AppFrontEngines.Integration.Tests.Common;

/// <summary>
/// Фикстура одна на весь сьют (коллекция xUnit): два отдельных IClassFixture-инстанса
/// поднимали два приложения в одном процессе и ломали друг друга через общее статическое состояние.
/// </summary>
public abstract class BaseAppFrontTests<TAppFixture> where TAppFixture : ApplicationFixture
{
    protected readonly TAppFixture AppFixture;
    //protected MarsDbContext DbContext => AppFixture.DbFixture.DbContext;

    public IFixture _fixture = new Fixture();

    protected BaseAppFrontTests(TAppFixture appFixture)
    {
        AppFixture = appFixture;
        AppFixture.DbFixture.Reset().RunSync();
        AppFixture.Seed().RunSync();
        AppFixture.ResetMocks();
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
