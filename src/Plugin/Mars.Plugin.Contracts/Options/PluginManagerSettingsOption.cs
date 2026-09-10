using System.ComponentModel.DataAnnotations;

namespace Mars.Plugin.Contracts.Options;

[Display(Name = "Настройки менеджера плагинов")]
public class PluginManagerSettingsOption
{
    [Display(Name = "Разрешить загрузку zip-файлов плагинов вручную")]
    public bool AllowUploadZipManually { get; set; } = true;

    [Display(Name = "Источники nuget для установки плагинов (через запятую)")]
    public string NugetSources { get; set; } = PluginNugetSourceDefaults.NugetOrgV3;

    [Display(Name = "Запрещённые плагины (packageId через запятую) — не ставятся и не грузятся")]
    public string BlockedPackageIds { get; set; } = string.Empty;

    public IEnumerable<string> GetNugetSources()
        => NugetSources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public HashSet<string> GetBlockedPackageIds()
        => [.. BlockedPackageIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}

public static class PluginNugetSourceDefaults
{
    public const string NugetOrgV3 = "https://api.nuget.org/v3/index.json";
}
