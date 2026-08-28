using Mars.Options.Services;
using Mars.Server.Abstractions.Startup;
using Mars.SiteEngine.Contracts.Options;
using Mars.SiteEngine.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mars.SiteEngine.Services;

/// <summary>
/// Прогрев рендера после запуска (FrontsOption.WarmupRenderOnStartup):
/// собирает движок первого фронта и сканирует шаблоны, чтобы первый запрос был быстрее.
/// </summary>
public class FrontRenderWarmupService(
    IOptionService optionService,
    IWebRenderEngineLocator renderEngineLocator,
    ILogger<FrontRenderWarmupService> logger) : IMarsAppLifetimeService
{
    public Task OnStartupAsync()
    {
        try
        {
            var option = optionService.GetOption<FrontsOption>();
            if (!option.WarmupRenderOnStartup)
                return Task.CompletedTask;

            // пока прогреваем только первый фронт
            var app = renderEngineLocator.GetAppFrontForUrl("/");
            if (app is null)
            {
                logger.LogWarning("Warmup render: фронт для url '/' не найден");
                return Task.CompletedTask;
            }

            logger.LogInformation("Warmup render: фронт '{Slug}' прогрет", app.Front?.Slug);
        }
        catch (Exception ex)
        {
            // прогрев — оптимизация, ошибки старта ломать не должны
            logger.LogWarning(ex, "Warmup render failed");
        }

        return Task.CompletedTask;
    }
}
