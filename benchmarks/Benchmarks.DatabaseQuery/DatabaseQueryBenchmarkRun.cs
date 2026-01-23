using AutoFixture;
using BenchmarkDotNet.Attributes;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.QueryLang.Services;
using Mars.Host.Shared.Services;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks.DatabaseQuery;

[KeepBenchmarkFiles]
[InProcess]
[MemoryDiagnoser]
public class DatabaseQueryBenchmarkRun : BenchmarkBase
{
    IServiceProvider _serviceProvider => AppFixture.ServiceProvider;

    /*
    Все еще есть проблема. Но это в самом тесте.

    System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.InvalidOperationException: The instance of entity type 'UserEntity' cannot be tracked because another instance with the same key value for {'Id'} is already being tracked. When attaching existing entities, ensure that only one entity instance with a given key value is attached. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see the conflicting key values.
     */

    [GlobalSetup]
    public async Task Setup()
    {
        await base.GlobalSetup();
        IMetaModelTypesLocator mlocator = _serviceProvider.GetRequiredService<IMetaModelTypesLocator>();
        mlocator.TryUpdateMetaModelMtoRuntimeCompiledTypes();

        await AppFixture.Seed();

        // Прогрев.
        var ef = AppFixture.MarsDbContext();
        IFixture _fixture = new Fixture();
        _fixture.Customize(new FixtureCustomize());

        var posts = _fixture.CreateMany<PostEntity>(20).ToList();
        ef.Posts.AddRange(posts);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
    }

    [Benchmark(Baseline = true)]
    public async Task Query()
    {
        var ef = AppFixture.MarsDbContext();
        var posts = await ef.Posts.Take(20).ToListAsync();
        if (posts.Count() != 20) throw new Exception("post.Count is not 20");
    }

    [Benchmark]
    public async Task Query_AsNoTracking()
    {
        var ef = AppFixture.MarsDbContext();
        var posts = await ef.Posts.AsNoTracking().Take(20).ToListAsync();
        if (posts.Count() != 20) throw new Exception("post.Count is not 20");
    }

    [Benchmark]
    public async Task Query_DynamicQuery()
    {
        //using var ef = TestMarsDbContext.GetEfContext();
        string query = "Post.Take(20)";
        //XInterpreter ppt = new();
        //EntityQuery eq = new EntityQuery(_serviceProvider, ppt, null);
        //IEnumerable<IBasicEntity> posts = (eq.Query(query) as IEnumerable<IBasicEntity>)!;
        //if (posts.Count() != 20) throw new Exception("post.Count is not 20");

        var handler = AppFixture.ServiceProvider.GetRequiredService<IQueryLangLinqDatabaseQueryHandler>();
        var posts = await handler.Handle(query, new(), default) as List<PostEntity>;
        if (posts!.Count() != 20) throw new Exception("post.Count is not 20");
    }

}
