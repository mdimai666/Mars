using System.Text;
using Mars.Host.Shared.Services;
using Mars.Shared.Options;
using Mars.UseStartup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Services;

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
    /// Чистый старт: если фронтов нет совсем — создаёт дефолтный фронт из стартового шаблона
    /// </summary>
    public static void EnsureDefaultFront(this IServiceProvider services)
    {
        if (MarsStartupInfo.IsTesting) return;

        var optionService = services.GetRequiredService<IOptionService>();
        var option = optionService.GetOption<FrontsOption>();
        if (option.Fronts.Count != 0) return;

        const string slug = FrontTemplateService.DefaultTemplateName;

        try
        {
            var templateService = services.GetRequiredService<FrontTemplateService>();
            templateService.CreateFrontFromTemplate(slug);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AppFront: create default front '{slug}' failed: {ex.Message}");
            return;
        }

        option.Fronts.Add(new FrontItem
        {
            Slug = slug,
            Title = slug,
            Url = "",
            Path = "",
            EngineId = FrontItem.HandlebarsEngine,
            Enabled = true,
        });
        optionService.SaveOption(option);

        Console.WriteLine($"AppFront: created default front '{slug}' from starter template");
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
