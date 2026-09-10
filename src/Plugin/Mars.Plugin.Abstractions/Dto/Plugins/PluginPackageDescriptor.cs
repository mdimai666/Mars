namespace Mars.Plugin.Abstractions.Dto.Plugins;

/// <summary>
/// Дескриптор установленного плагина (`mars-plugin.json` в корне папки плагина).
/// Пишется инструментом паковки `Mars.Plugin.Sdk`, читается рантаймом и инсталлерами.
/// </summary>
public class PluginPackageDescriptor
{
    public const string FileName = "mars-plugin.json";
    public const string MarsPluginPackageType = "MarsPlugin";

    public string PackageType { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string EntryAssembly { get; set; } = string.Empty;

    /// <summary>
    /// Версия Марса, инструментом которой собран пакет (нижняя граница совместимости).
    /// </summary>
    public string MarsVersion { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>Отображаемое имя (из метаданных пакета; атрибуты сборки — фолбэк).</summary>
    public string? Title { get; set; }

    /// <summary>Описание (из метаданных пакета; атрибуты сборки — фолбэк).</summary>
    public string? Description { get; set; }

    /// <summary>Имя файла иконки в `wwwroot/` папки плагина (сервится через `/_plugin/&lt;key&gt;/`).</summary>
    public string? IconFile { get; set; }
}
