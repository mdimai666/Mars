using AppShared.Models;
using Mars.Host.Data;
using Mars.Host.QueryLang;
using Mars.Host.Shared.Templators;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.Host.TestHostApp;

namespace Benchmarks.DatabaseQuery;

public class TestAppRun : UnitTestHostBaseClass
{
    public string Call1()
    {
        var ef = _serviceProvider.GetRequiredService<MarsDbContextLegacy>();
        var count = ef.Posts.Count();
        return $"posts = {count}";
    }

    public string Call2()
    {
        string query = "Post.Take(20)";
        XInterpreter ppt = new();
        EntityQuery eq = new EntityQuery(_serviceProvider, ppt, null);
        var res = eq.Query(query);
        IEnumerable<IBasicEntity> posts = (res as IEnumerable<IBasicEntity>)!;
        if (posts.Count() != 20) throw new Exception("post.Count is not 20");

        var count = posts.Count();
        return $"posts = {count}";
    }
}
