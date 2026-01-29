using Mars.E2E.Tests.Fixtures;
using Mars.E2E.Tests.Helpers;
using Mars.Integration.Tests.Attributes;

namespace Mars.E2E.Tests.Screenshots;

public class CaptureDemoPages : BaseE2ETests
{
    public CaptureDemoPages(E2EServerFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task DemoPages_Screenshots()
    {
        var paths = new[]
        {
            "/dev/",
            "/dev/Profile",
            "/dev/Settings"
        };

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
}
