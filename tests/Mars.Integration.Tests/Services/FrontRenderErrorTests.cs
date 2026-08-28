using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Core.Models;
using Mars.SiteEngine.Contracts.Options;
using Mars.Nodes.Abstractions.Hubs;
using Mars.Server.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.WebSite.Interfaces;
using Mars.SiteEngine.Endpoints;
using Mars.SiteEngine.Handlebars;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Крайние случаи рендера фронта: несуществующая/пустая папка, битая структура шаблона —
/// ошибки должны быть диагностируемыми (с путём) и не улетать без сообщения.
/// </summary>
public class FrontRenderErrorTests : IDisposable
{
    readonly string dir;
    readonly ServiceProvider services;

    public FrontRenderErrorTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mars-front-error-tests", Guid.NewGuid().ToString("N"));

        var hub = Substitute.For<IHubContext<ChatHub>>();
        var clients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.All.Returns(clientProxy);

        services = new ServiceCollection()
            .AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()))
            .AddSingleton(hub)
            .BuildServiceProvider();
    }

    MarsAppFront CreateAppFront(string path) => new()
    {
        Configuration = new AppFrontSettingsCfg { Path = path, Url = "" },
        Front = new FrontItem { Slug = "test-front", Url = "" },
    };

    HandlebarsWebRenderEngine CreateEngine(MarsAppFront appFront)
    {
        var engine = new HandlebarsWebRenderEngine(services.GetRequiredService<IMemoryCache>(), appFront);
        engine.Setup();
        engine.InitializeEngine(services);
        return engine;
    }

    [Fact]
    public void Setup_NonExistentFolder_ThrowsDirectoryNotFoundWithPath()
    {
        Directory.CreateDirectory(dir);
        var missing = Path.Combine(dir, "missing");
        var engine = new HandlebarsWebRenderEngine(services.GetRequiredService<IMemoryCache>(), CreateAppFront(missing));

        var act = () => engine.Setup();

        act.Should().Throw<DirectoryNotFoundException>().WithMessage($"*{missing}*");
    }

    [Fact]
    public void Template_EmptyFolder_ThrowsWithPath()
    {
        Directory.CreateDirectory(dir);
        var appFront = CreateAppFront(dir);
        CreateEngine(appFront);
        var wts = appFront.Features.Get<IWebTemplateService>()!;

        var act = () => { _ = wts.Template; };

        act.Should().Throw<FileNotFoundException>().WithMessage($"*{dir}*");
    }

    [Fact]
    public void Template_MissingRoot_Throws()
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "index.hbs"), """
@page "/"

content
""");
        var appFront = CreateAppFront(dir);
        CreateEngine(appFront);
        var wts = appFront.Features.Get<IWebTemplateService>()!;

        var act = () => { _ = wts.Template; };

        act.Should().Throw<NotFoundException>().WithMessage("*Root*");
    }

    [Fact]
    public async Task Response_EmptyTemplateFolder_Returns500WithPathAndSlug()
    {
        Directory.CreateDirectory(dir);
        var appFront = CreateAppFront(dir);
        CreateEngine(appFront);

        var http = new DefaultHttpContext
        {
            RequestServices = services,
        };
        http.Items[nameof(MarsAppFront)] = appFront;
        http.Response.Body = new MemoryStream();

        await new MapWebSiteProcessor().Response(http, default);

        http.Response.StatusCode.Should().Be(500);
        http.Response.Body.Position = 0;
        var body = await new StreamReader(http.Response.Body).ReadToEndAsync();

        body.Should().Contain("test-front");
        body.Should().Contain(dir);
    }

    public void Dispose()
    {
        services.Dispose();
        try
        {
            var baseDir = Path.GetDirectoryName(dir)!;
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, true);
        }
        catch
        {
            // временная папка могла остаться под watcher'ом
        }
    }
}
