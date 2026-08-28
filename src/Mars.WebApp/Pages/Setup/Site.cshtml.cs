using Mars.Contracts.Options;
using Mars.Services;
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
    public string FrontChoice { get; set; } = FrontTemplateService.DefaultTemplateName;

    [BindProperty]
    public string FrontPath { get; set; } = "";

    [BindProperty]
    public string FrontEngineId { get; set; } = FrontItem.HandlebarsEngine;

    public IReadOnlyList<string> AvailableTemplates { get; private set; } = [];

    public SiteModel(SetupService setupService)
    {
        _setupService = setupService;
    }

    public static string TemplateTitle(string name) => name switch
    {
        FrontTemplateService.DefaultTemplateName => "Базовый сайт",
        FrontTemplateService.LandingTemplateName => "Лендинг",
        _ => name,
    };

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
        FrontChoice = _setupService.FrontChoice;
        FrontPath = _setupService.FrontPath;
        FrontEngineId = _setupService.FrontEngineId;
        AvailableTemplates = _setupService.GetAvailableFrontTemplates();

        return Page();
    }

    public IActionResult OnPost()
    {
        AvailableTemplates = _setupService.GetAvailableFrontTemplates();

        if (FrontChoice == SetupService.ExistingFrontChoice && string.IsNullOrWhiteSpace(FrontPath))
        {
            ModelState.AddModelError(nameof(FrontPath), "Укажите путь к папке с шаблонами");
            return Page();
        }

        // Save to service
        _setupService.SiteUrl = SiteUrl?.TrimEnd('/') ?? "";
        _setupService.SiteName = SiteName ?? "Mars";
        _setupService.SiteDescription = SiteDescription ?? "";
        _setupService.LoggingLevel = LoggingLevel ?? "Information";
        _setupService.FrontChoice = FrontChoice ?? FrontTemplateService.DefaultTemplateName;
        _setupService.FrontPath = FrontPath?.Trim() ?? "";
        _setupService.FrontEngineId = string.IsNullOrWhiteSpace(FrontEngineId) ? FrontItem.HandlebarsEngine : FrontEngineId;

        return RedirectToPage("/setup/user");
    }
}
