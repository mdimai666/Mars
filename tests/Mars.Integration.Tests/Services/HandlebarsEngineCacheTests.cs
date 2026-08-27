using System.Diagnostics;
using FluentAssertions;
using Mars.Core.Models;
using Mars.Nodes.Abstractions.Hubs;
using Mars.Server.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.Contracts.Options;
using Mars.SiteEngine.Handlebars;
using Mars.SiteEngine.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Скомпилированный Handlebars-шаблон не должен пересобираться на каждый запрос:
/// он кэшируется в IMemoryCache и кэш сбрасывается только после изменения файлов шаблона.
/// </summary>
public class HandlebarsEngineCacheTests : IDisposable
{
    readonly string dir;
    readonly ServiceProvider services;
    readonly MarsAppFront appFront;
    readonly HandlebarsWebRenderEngine engine;

    public HandlebarsEngineCacheTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mars-hbs-cache-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "_root.hbs"), "@Body");
        File.WriteAllText(IndexFile(), """
@page "/"

v1
""");

        var hub = Substitute.For<IHubContext<ChatHub>>();
        var clients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.All.Returns(clientProxy);

        services = new ServiceCollection()
            .AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()))
            .AddSingleton(hub)
            .BuildServiceProvider();

        appFront = new MarsAppFront
        {
            Configuration = new AppFrontSettingsCfg
            {
                Path = dir,
                Url = "",
            },
        };

        // как HandlebarsRenderEngineFactory.Create: движок + WebTemplateService с подпиской на изменения
        engine = new HandlebarsWebRenderEngine(services.GetRequiredService<IMemoryCache>(), appFront);
        engine.Setup();
        engine.InitializeEngine(services);
    }

    string IndexFile() => Path.Combine(dir, "index.hbs");

    WebTemplateService Wts() => (WebTemplateService)appFront.Features.Get<Mars.SiteEngine.Abstractions.WebSite.Interfaces.IWebTemplateService>()!;

    string Render()
    {
        var template = Wts().Template;
        var ctx = new PageRenderContext
        {
            Request = new WebClientRequest(new Uri("http://localhost/")),
            SysOptions = new SysOptions { SiteUrl = "http://localhost" },
            User = null,
            IsDevelopment = false,
            RenderParam = new RenderParam(), // UseCache = true
            TemplateContextVaribles = new(),
        };

        return engine.RenderPage(appFront, ctx, template.Roots.Values.First(), template.IndexPage, template.Parts, services, default);
    }

    static WebSiteTemplate InMemoryTemplate(string indexContent) => new(new[]
    {
        new WebPartSource("@Body", "_root.hbs", "", "", ""),
        new WebPartSource("@page /\n\n" + indexContent, "index.hbs", "", "", ""),
    });

    [Fact]
    public void CompiledTemplate_IsCached_UntilClearCache()
    {
        Render().Should().Contain("v1");

        // шаблон подменён без файлового события — кэш не сбрасывался
        Wts().Template = InMemoryTemplate("v2");

        Render().Should().Contain("v1", "скомпилированный шаблон должен браться из кэша, а не перекомпилироваться");

        Wts().ClearCache();

        Render().Should().Contain("v2", "после сброса кэша шаблон перекомпилируется из актуального источника");
    }

    [Fact]
    public async Task FileChange_ClearsCompiledCache_EndToEnd()
    {
        Render().Should().Contain("v1");

        File.WriteAllText(IndexFile(), """
@page "/"

v2
""");

        // watcher → debounce → перескан → OnFileUpdated → ClearCache → перекомпиляция
        var updated = await WaitUntil(() => Render().Contains("v2"));
        updated.Should().BeTrue("после изменения файла рендер должен отдавать новый контент");
    }

    static async Task<bool> WaitUntil(Func<bool> condition, int timeoutMs = 10_000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition()) return true;
            await Task.Delay(100);
        }
        return condition();
    }

    public void Dispose()
    {
        services.Dispose();
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch
        {
            // временная папка могла остаться под watcher'ом
        }
    }
}
