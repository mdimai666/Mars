using Mars.Options.Services;
using Mars.SiteEngine.Contracts.Options;
using Mars.Test.Common.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests;

/// <summary>
/// После реворка фронтов (file-based fronts + FrontsOption в БД) в тестовом окружении
/// фронт не создаётся (EnsureDefaultFront пропускает тесты) — регистрируем файловую тему,
/// иначе Front/PageRender отдают 404 (фронт для url не найден).
/// </summary>
public static class TestFrontHelper
{
    public const string FrontSlug = "webclient-front";

    public static void EnsureFront(IServiceProvider serviceProvider)
    {
        var optionService = serviceProvider.GetRequiredService<IOptionService>();
        var option = optionService.GetOption<FrontsOption>();
        if (option.Fronts.Any(s => s.Slug == FrontSlug)) return;

        var themePath = SolutionPathHelper.Resolve("tests", "Mars.WebApiClient.Integration.Tests", "appTheme");
        option.Fronts.Add(new FrontItem
        {
            Slug = FrontSlug,
            Title = FrontSlug,
            Url = "",
            Path = themePath,
            EngineId = FrontItem.HandlebarsEngine,
            Enabled = true,
        });
        optionService.SaveOption(option);
    }
}
