using System.ComponentModel.DataAnnotations;

namespace Mars.Options.Models;

[Display(Name = "Режим обслуживания")]
public class MaintenanceModeOption
{

    [Display(Name = "Включить режим обслуживания")]
    public bool Enable { get; set; }

    [Display(Name = "PageSource")]
    public EMaintenancePageSource MaintenancePageSource { get; set; } = EMaintenancePageSource.StaticHtml;

    string _maintenanceStaticPage = null!;

    [Display(Name = "Содержимое статической страницы", GroupName = "StaticPage")]
    public string MaintenanceStaticPageContent { get => _maintenanceStaticPage ?? DefaultMaintenanceStaticPage(); set => _maintenanceStaticPage = value; }

    [Display(Name = "Титл страницы")]
    public string MaintenanceStaticPageTitle { get; set; } = "Сайт отключен";

    public string DefaultMaintenanceStaticPage() => """
        <!DOCTYPE html>
        <html lang="ru">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Сайт отключен</title>
        </head>
        <body style="display: flex;height:100vh;width: 100vw;padding: 0; margin: 0;">
            <div style="display:flex;flex: 1 1; align-items: center;justify-content: center;">
                <h1 style="font-family: Arial, Helvetica, sans-serif;">Сайт отключен</h1>
            </div>
        </body>
        </html>
        """;

    [Display(Name = "RenderPageUrl")]
    public string RenderPageUrl { get; set; } = "";
}

public enum EMaintenancePageSource
{
    StaticHtml,
    PostPage
}

