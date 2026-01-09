using AppShared.Models;
using BenchmarkDotNet.Attributes;
using Mars.Host.QueryLang;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.Mars.Host.TestHostApp;

namespace Benchmarks.DatabaseQuery;

[MemoryDiagnoser, ShortRunJob]
public class DatabaseQueryBenchmarkRun : UnitTestHostBaseClass
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        IMetaModelTypesLocator mlocator = _serviceProvider.GetRequiredService<IMetaModelTypesLocator>();
        mlocator.TryUpdateMetaModelMtoRuntimeCompiledTypes(_serviceProvider);
    }

    [Benchmark(Baseline = true)]
    public void Query()
    {
        using var ef = TestMarsDbContext.GetEfContext(_serviceProvider);
        var posts = ef.Posts.Take(20).ToList();
        if (posts.Count() != 20) throw new Exception("post.Count is not 20");
    }

    [Benchmark]
    public void Query_AsNoTracking()
    {
        using var ef = TestMarsDbContext.GetEfContext(_serviceProvider);
        var posts = ef.Posts.AsNoTracking().Take(20).ToList();
        if (posts.Count() != 20) throw new Exception("post.Count is not 20");
    }

    [Benchmark]
    public void Query_DynamicQuery()
    {
        //using var ef = TestMarsDbContext.GetEfContext();
        string query = "Post.Take(20)";
        XInterpreter ppt = new();
        EntityQuery eq = new EntityQuery(_serviceProvider, ppt, null);
        IEnumerable<IBasicEntity> posts = (eq.Query(query) as IEnumerable<IBasicEntity>)!;
        if (posts.Count() != 20) throw new Exception("post.Count is not 20");
    }

}
