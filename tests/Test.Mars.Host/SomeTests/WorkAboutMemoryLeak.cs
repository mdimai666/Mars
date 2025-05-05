using System.Diagnostics;
using Mars.Host.Data;
using Mars.Host.QueryLang;
using Mars.Host.Shared.Templators;
using Mars.WebSiteProcessor.Interfaces;
using Mars.Core.Extensions;
using Remote.Linq.Include;
using Test.Mars.Host.TestHostApp;
using Xunit.Abstractions;

namespace Test.Mars.Host.SomeTests;

public class WorkAboutMemoryLeak : UnitTestHostBaseClass
{
    private readonly ITestOutputHelper output = default!;

    public WorkAboutMemoryLeak(ITestOutputHelper output)
    {
        this.output = output;
    }

    void _Begin()
    {
        long total0 = GC.GetTotalAllocatedBytes();
        this.total0 = total0;
        stopwatch = Stopwatch.StartNew();
    }

    long total0;
    long total2;
    long userMemory;
    Stopwatch stopwatch = default!;
    double elapsedMillis;

    void _End()
    {
        stopwatch.Stop();
        long total2 = GC.GetTotalAllocatedBytes();
        string mem = (total2 - total0).ToHumanizedSize();
        this.total2 = total2;
        userMemory = total2 - total0;
        elapsedMillis = stopwatch.ElapsedMilliseconds;
        string elapsed = stopwatch.Elapsed.TotalMilliseconds + "ms";
        output.WriteLine($"memory: {mem}\telapsed: {elapsed}");
    }

    [Fact]
    public void TestTotalEmpty()
    {
        var ctx = _renderContext();
        IWebRenderEngine renderEngine = MarsAppFront.Features.Get<IWebRenderEngine>()!;

        _Begin();

        ctx.Page.Content = "x";
        string html = renderEngine.RenderPage(ctx);

        _End();

        Assert.Equal("x", html);
        Assert.True(userMemory < 512 * 1024);//less than 512kb
        Assert.True(elapsedMillis < 70);//less than 70ms
    }

    [Fact]
    public void TestMemotyLoop10InRenderPage()
    {
        string ctxQuery = "";

        for (int i = 0; i < 10; i++)
        {
            ctxQuery += $"_=post.Where(post.Title!=\"_{i}_\").Take(10).Fill()\n";//чтобы не кешировались запросы
        }

        var ctx = _renderContext();
        IWebRenderEngine renderEngine = MarsAppFront.Features.Get<IWebRenderEngine>()!;

        _Begin();

        ctx.Page.Content = "{{#context}}\n" + ctxQuery + "{{/context}}\n\n";
        string html = renderEngine.RenderPage(ctx);

        _End();

        Assert.True(userMemory < 2 * 1024 * 1024, "memory leak");

    }

    [Fact]
    public void TestMemoryUsegeOnlyQueryLang()
    {
        //IQueryLangProcessing queryLang = _serviceProvider.GetRequiredService<IQueryLangProcessing>();
        //queryLang.Process

        var ctx = _context();
        XInterpreter ppt = new(ctx);

        _Begin();

        for (int i = 0; i < 10; i++)
        {
            string key = "key" + i;
            string val = $"post.Where(post.Title!=\"_{i}_\").Take(10).Fill()";
            int index = i;
            var result = EfDynamicQueryHelper2.Query(key, val, index, ctx, ppt);
        }
        GC.Collect();
        GC.WaitForFullGCApproach();

        _End();

        Assert.True(userMemory < 2 * 1024 * 1024, "memory leak");
    }

    [Fact]
    public void TestPureMemoryEntityFramework()
    {
        //var ctx = _context();
        //XInterpreter ppt = new(ctx);

        _Begin();

        int gen1;

        using (var ef = MarsDbContextLegacy.CreateInstance())
        {
            for (int i = 0; i < 10; i++)
            {
                string key = "key" + i;
                string val = $"post.Where(post.Title!=\"_{i}_\").Take(10).Fill()";
                int index = i;
                //var result = EfDynamicQueryHelper2.Query(key, val, index, ctx, ppt).Result;
                var result = ef.Posts.Where(s => s.Title != $"_{i}_").Include(s => s.User).ToList();
            }

            gen1 = GC.GetGeneration(ef);
        }
        GC.Collect(gen1);
        GC.WaitForFullGCApproach();
        GC.WaitForPendingFinalizers();



        _End();

        var UsedMemory = Process.GetCurrentProcess().PagedMemorySize64;
        var mem = UsedMemory.ToHumanizedSize();

        output.WriteLine($"mem2 = {mem}");

        Assert.True(userMemory < 2 * 1024 * 1024, "memory leak");
    }
}
