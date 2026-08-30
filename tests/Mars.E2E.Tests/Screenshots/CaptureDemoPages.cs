using Mars.Admin.Framework.Interfaces;
using Mars.E2E.Tests.Fixtures;
using Mars.E2E.Tests.Helpers;
using Mars.Integration.Tests.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.E2E.Tests.Screenshots;

public class CaptureDemoPages : BaseE2ETests
{
    // Админка смонтирована на /dev (StartupDevAdmin), маршруты страниц идут без префикса
    private const string AdminMountPrefix = "/dev";

    public CaptureDemoPages(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact(Skip = SkipE2ETests)]
    public async Task DemoPages_Screenshots()
    {
        var paths = GetAdminPagePaths();

        var viewports = new[]
        {
            Viewports.Desktop,
            Viewports.Mobile
        };

        var crawler = new ScreenshotCrawler(Context, BaseUrl);

        await crawler.CaptureAsync(
            paths,
            viewports,
            outputDir: "screenshots");
    }

    /// <summary>
    /// Пути всех страниц админки без параметров маршрута: берём из IBlazorPagesService,
    /// на страницу выбираем самый короткий маршрут, добавляем префикс маунта /dev.
    /// Logout ставим последним — он разрушает сессию.
    /// </summary>
    private List<string> GetAdminPagePaths()
    {
        var pagesService = AppFixture.ServiceProvider.GetRequiredService<IBlazorPagesService>();

        return pagesService.GetStaticRoutedPages([typeof(Mars.Admin.App).Assembly])
            .Select(p => p.Routes
                .Where(r => !r.Contains('{'))
                .OrderBy(r => r.Length)
                .First())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(r => AdminMountPrefix + r)
            .OrderBy(p => p.Contains("logout", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ToList();
    }
}
