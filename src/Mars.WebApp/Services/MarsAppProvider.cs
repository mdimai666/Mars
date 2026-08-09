using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.WebSiteProcessor.Interfaces;

namespace Mars.Services;

/// <summary>
/// Фасад над IFrontManager/IWebRenderEngineLocator для старых потребителей IMarsAppProvider.
/// Раньше читал секцию AppFront из appsettings один раз при старте; теперь список фронтов
/// живёт в опции FrontsOption и применяется в рантайме.
/// </summary>
public class MarsAppProvider : IMarsAppProvider
{
    readonly IServiceProvider services;
    IFrontManager? frontManager;
    IWebRenderEngineLocator? locator;

    public MarsAppProvider(IServiceProvider services)
    {
        this.services = services;
    }

    IFrontManager FrontManager => frontManager ??= services.GetRequiredService<IFrontManager>();
    IWebRenderEngineLocator Locator => locator ??= services.GetRequiredService<IWebRenderEngineLocator>();

    public IReadOnlyDictionary<string, MarsAppFront> Apps
    {
        get
        {
            var dict = new Dictionary<string, MarsAppFront>();
            foreach (var front in FrontManager.Fronts)
            {
                if (!front.Enabled) continue;

                var app = Locator.GetAppFrontForUrl(string.IsNullOrEmpty(front.Url) ? "/" : front.Url);
                if (app is not null) dict[front.Url] = app;
            }
            return dict;
        }
    }

    public MarsAppFront FirstApp => GetAppForUrl("/");

    public bool SetupMultiApps => FrontManager.Fronts.Count(s => s.Enabled) > 1;

    public MarsAppFront GetAppForUrl(string url)
    {
        return Locator.GetAppFrontForUrl(url)
            ?? throw new InvalidOperationException($"Фронт для url '{url}' не найден. Проверьте FrontsOption (настройки фронтов).");
    }

    public MarsAppFront GetAppBySlug(string slug)
    {
        return Locator.GetAppFrontBySlug(slug)
            ?? throw new InvalidOperationException($"Фронт со slug '{slug}' не найден. Проверьте FrontsOption (настройки фронтов).");
    }
}
