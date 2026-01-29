using Microsoft.Playwright;

namespace Mars.E2E.Tests.Helpers;

public sealed class ScreenshotCrawler
{
    private readonly IBrowserContext _context;
    private readonly IPage _page;
    private readonly string _baseUrl;

    public ScreenshotCrawler(IBrowserContext context, string baseUrl)
    {
        _context = context;
        _baseUrl = baseUrl;
        _page = context.NewPageAsync().GetAwaiter().GetResult();
    }

    public async Task CaptureAsync(
        IEnumerable<string> paths,
        IEnumerable<Viewport> viewports,
        string outputDir)
    {
        if (Directory.Exists(outputDir)) Directory.Delete(outputDir);
        Directory.CreateDirectory(outputDir);

        foreach (var viewport in viewports)
        {
            await _page.SetViewportSizeAsync(viewport.Width, viewport.Height);

            foreach (var path in paths)
            {
                var url = _baseUrl + path;

                await _page.GotoAsync(url, new()
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                await _page.WaitForTimeoutAsync(300); // стабилизация SPA

                var fileName = $"{viewport.Name}_{path.Trim('/').Replace('/', '_')}.png";
                var fullPath = Path.Combine(outputDir, fileName);

                await _page.ScreenshotAsync(new()
                {
                    Path = fullPath,
                    FullPage = true
                });
            }
        }
    }
}

public record Viewport(string Name, int Width, int Height);

public static class Viewports
{
    public static readonly Viewport Desktop = new("desktop", 1920, 1080);
    public static readonly Viewport Laptop = new("laptop", 1366, 768);
    public static readonly Viewport Tablet = new("tablet", 768, 1024);
    public static readonly Viewport Mobile = new("mobile", 390, 844);
}
