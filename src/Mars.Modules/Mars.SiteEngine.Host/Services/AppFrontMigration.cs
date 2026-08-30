using System.Text;
using Mars.Options.Abstractions.Services;
using Mars.SiteEngine.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mars.SiteEngine.Host.Services;

/// <summary>
/// Разовая миграция фронтов из секции "AppFront" (appsettings) в опцию FrontsOption
/// и бутстрап дефолтного фронта при чистом старте.
/// </summary>
public static class AppFrontMigration //TODO: аотом убрать
{
    /// <summary>
    /// Легаси-запись секции "AppFront" из appsettings (до FrontsOption).
    /// Mode — строка с именем значения старого перечисления AppFrontMode.
    /// </summary>
    internal class LegacyAppFrontCfg
    {
        public string Path { get; set; } = "";
        public string Url { get; set; } = "";
        public string? Mode { get; set; }
    }

    public static void MigrateAppFrontToOption(this IServiceProvider services, IConfiguration configuration)
    {
        var optionService = services.GetRequiredService<IOptionService>();

        var option = optionService.GetOption<FrontsOption>();
        if (option.Fronts.Count != 0) return;

        var fronts = MapToOption(ReadConfig(configuration)).Fronts;
        if (fronts.Count == 0) return;

        option.Fronts = fronts;
        optionService.SaveOption(option);

        Console.WriteLine($"AppFrontMigration: migrated {fronts.Count} front(s) from appsettings 'AppFront' to FrontsOption");
    }

    /// <summary>
    /// Чистый старт: если фронтов нет совсем — создаёт фронт по выбору из визарда
    /// (Setup:FrontChoice/FrontPath/FrontEngineId): стартовый шаблон (default/landing/...)
    /// или подключение существующей папки с шаблонами; без выбора — дефолтный шаблон.
    /// </summary>
    public static void EnsureDefaultFront(this IServiceProvider services, IConfiguration configuration)
    {
        var hostEnvironment = services.GetRequiredService<IHostEnvironment>();
        if (hostEnvironment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase)) return;

        var optionService = services.GetRequiredService<IOptionService>();
        var option = optionService.GetOption<FrontsOption>();
        if (option.Fronts.Count != 0) return;

        // визард: существующая папка с шаблонами (путь + движок)
        var frontPath = configuration["Setup:FrontPath"];
        if (!string.IsNullOrWhiteSpace(frontPath))
        {
            var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(frontPath));
            var engineId = configuration["Setup:FrontEngineId"];

            option.Fronts.Add(new FrontItem
            {
                Slug = MakeSlug(folderName, []),
                Title = folderName,
                Url = "",
                Path = frontPath,
                EngineId = string.IsNullOrWhiteSpace(engineId) ? FrontItem.HandlebarsEngine : engineId,
                Enabled = true,
            });
            optionService.SaveOption(option);

            Console.WriteLine($"AppFront: attached existing front folder '{frontPath}'");
            return;
        }

        // визард: стартовый шаблон (фолбек на default, если выбранного нет)
        var templateService = services.GetRequiredService<FrontTemplateService>();
        var templateName = configuration["Setup:FrontChoice"];
        if (string.IsNullOrWhiteSpace(templateName)
            || templateName == FrontsOption.ExistingFrontChoice
            || !Directory.Exists(templateService.GetTemplatePath(templateName)))
        {
            templateName = FrontTemplateService.DefaultTemplateName;
        }

        try
        {
            templateService.CreateFrontFromTemplate(templateName, templateName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AppFront: create front from template '{templateName}' failed: {ex.Message}");
            return;
        }

        option.Fronts.Add(new FrontItem
        {
            Slug = templateName,
            Title = templateName,
            Url = "",
            Path = "",
            EngineId = FrontItem.HandlebarsEngine,
            Enabled = true,
        });
        optionService.SaveOption(option);

        Console.WriteLine($"AppFront: created front '{templateName}' from starter template");
    }

    internal static FrontsOption MapToOption(IReadOnlyCollection<LegacyAppFrontCfg> items)
    {
        var option = new FrontsOption();
        var takenSlugs = new List<string>();

        foreach (var cfg in items)
        {
            if (cfg is null) continue;

            // None и Blazor-режимы (ServeStaticBlazor/BlazorPrerender) не мигрируются —
            // это режимы старого рендера, Blazor-рендер это отдельная задача
            if (!IsHandlebarsMode(cfg.Mode))
                continue;

            var slug = MakeSlug(cfg.Url, takenSlugs);
            takenSlugs.Add(slug);

            option.Fronts.Add(new FrontItem
            {
                Slug = slug,
                Title = slug,
                Url = cfg.Url,
                Path = cfg.Path,
                EngineId = FrontItem.HandlebarsEngine,
                Enabled = true,
            });
        }

        return option;
    }

    static bool IsHandlebarsMode(string? mode)
        => string.IsNullOrWhiteSpace(mode)
        || mode.Equals("HandlebarsTemplate", StringComparison.OrdinalIgnoreCase)
        || mode.Equals("HandlebarsTemplateStatic", StringComparison.OrdinalIgnoreCase);

    internal static string MakeSlug(string? url, IReadOnlyCollection<string> takenSlugs)
    {
        var raw = (url ?? "").Trim('/').Replace('/', '-');
        if (raw.Length == 0) raw = "default";

        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (char.IsLetterOrDigit(ch) || ch is '_' or '-')
                sb.Append(char.ToLowerInvariant(ch));
        }

        var slug = sb.Length > 0 ? sb.ToString() : "front";

        var result = slug;
        int num = 1;
        while (takenSlugs.Contains(result))
        {
            result = $"{slug}{num++}";
        }

        return result;
    }

    static LegacyAppFrontCfg[] ReadConfig(IConfiguration configuration)
    {
        var section = configuration.GetSection("AppFront");
        if (!section.Exists()) return [];

        var rootElementHasModeField = section.GetValue<string?>("Mode") is not null;

        return rootElementHasModeField
            ? [section.Get<LegacyAppFrontCfg>()!]
            : section.Get<LegacyAppFrontCfg[]>() ?? [];
    }
}
