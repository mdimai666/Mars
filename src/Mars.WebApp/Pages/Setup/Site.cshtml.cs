using Mars.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mars.Pages.Setup;

public class SiteModel : PageModel
{
    private readonly SetupService _setupService;

    [BindProperty]
    public string SiteUrl { get; set; } = "";

    [BindProperty]
    public string SiteName { get; set; } = "Mars";

    [BindProperty]
    public string SiteDescription { get; set; } = "";

    [BindProperty]
    public string LoggingLevel { get; set; } = "Information";

    [BindProperty]
    public string AppFrontMode { get; set; } = "HandlebarsTemplate";

    [BindProperty]
    public string AppFrontStaticPath { get; set; } = "../client";

    public SiteModel(SetupService setupService)
    {
        _setupService = setupService;
    }

    public IActionResult OnGet()
    {
        // Auto-fill SiteUrl from browser address if not set
        if (string.IsNullOrEmpty(SiteUrl))
        {
            var request = HttpContext.Request;
            SiteUrl = $"{request.Scheme}://{request.Host}";
        }

        // Restore from service if going back
        SiteUrl = _setupService.SiteUrl.Length > 0 ? _setupService.SiteUrl : SiteUrl;
        SiteName = _setupService.SiteName;
        SiteDescription = _setupService.SiteDescription;
        LoggingLevel = _setupService.LoggingLevel;
        AppFrontMode = _setupService.AppFrontMode;
        AppFrontStaticPath = _setupService.AppFrontStaticPath;

        return Page();
    }

    public IActionResult OnPost()
    {
        // Save to service
        _setupService.SiteUrl = SiteUrl?.TrimEnd('/') ?? "";
        _setupService.SiteName = SiteName ?? "Mars";
        _setupService.SiteDescription = SiteDescription ?? "";
        _setupService.LoggingLevel = LoggingLevel ?? "Information";
        _setupService.AppFrontMode = AppFrontMode ?? "HandlebarsTemplate";
        _setupService.AppFrontStaticPath = AppFrontStaticPath ?? "../client";

        return RedirectToPage("/setup/user");
    }
}
