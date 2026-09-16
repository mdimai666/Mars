using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Providers.File;

/// <summary>Настройки файлового источника из <see cref="DatasourceConfig.Settings"/>.</summary>
public class FileSourceSettings
{
    public const string FileKey = DatasourceSettings.File;
    public const string HasHeadersKey = DatasourceSettings.HasHeaders;
    public const string DelimiterKey = DatasourceSettings.Delimiter;

    /// <summary>Файл по умолчанию, когда запрос не указал объект; пусто — единственный файл источника.</summary>
    public string File { get; init; } = "";

    public bool HasHeaders { get; init; } = true;

    /// <summary>Разделитель CSV; пусто — определить по первой строке.</summary>
    public string? Delimiter { get; init; }

    public static FileSourceSettings From(DatasourceConfig config)
    {
        var settings = config.Settings ?? [];

        return new FileSourceSettings
        {
            File = Get(settings, FileKey),
            HasHeaders = !string.Equals(Get(settings, HasHeadersKey), "false", StringComparison.OrdinalIgnoreCase),
            Delimiter = NullIfEmpty(Get(settings, DelimiterKey)),
        };
    }

    static string Get(Dictionary<string, string> settings, string key)
        => settings.TryGetValue(key, out var value) ? value?.Trim() ?? "" : "";

    static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}
