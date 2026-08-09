using System.Diagnostics;
using FluentAssertions;
using Mars.Core.Models;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Models;
using Mars.WebSiteProcessor.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Изменения файлов фронта должны подхватываться FileSystemWatcher'ом (с debounce),
/// без перезапуска приложения.
/// </summary>
public class WebTemplateServiceWatcherTests : IDisposable
{
    readonly string dir;
    readonly ServiceProvider services;
    readonly WebTemplateService wts;

    public WebTemplateServiceWatcherTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mars-wts-watcher-tests", Guid.NewGuid().ToString("N"));
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
            .BuildServiceProvider();

        wts = new WebTemplateService(services, hub, CreateAppFront(dir));
    }

    string IndexFile() => Path.Combine(dir, "index.hbs");

    static MarsAppFront CreateAppFront(string path) => new()
    {
        Configuration = new AppFrontSettingsCfg
        {
            Path = path,
            Url = "",
            Mode = AppFrontMode.HandlebarsTemplateStatic,
        },
    };

    [Fact]
    public async Task FileChanged_TemplateUpdated()
    {
        wts.Template.Pages.Should().Contain(p => p.Content.Contains("v1"));

        File.WriteAllText(IndexFile(), """
@page "/"

v2
""");

        var updated = await WaitUntil(() => wts.Template.Pages.Any(p => p.Content.Contains("v2")));
        updated.Should().BeTrue("watcher должен перечитать шаблон после изменения файла");
    }

    [Fact]
    public async Task ManyRapidChanges_ConvergesToLastVersion()
    {
        for (int i = 1; i <= 5; i++)
        {
            File.WriteAllText(IndexFile(), $"""
@page "/"

burst_{i}
""");
            await Task.Delay(30);
        }

        var updated = await WaitUntil(() => wts.Template.Pages.Any(p => p.Content.Contains("burst_5")));
        updated.Should().BeTrue("после серии правок debounce должен привести к последней версии");
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
