namespace Mars.Datasource.Contracts.Models;

/// <summary>
/// Ключи <see cref="DatasourceConfig.Settings"/>: по ним форма настроек и провайдер источника
/// договариваются без общих типов. Провайдер может читать и свои ключи, эти — общие для формы.
/// </summary>
public static class DatasourceSettings
{
    /// <summary>Файлы источника через ';': путь в медиа-хранилище или абсолютный путь на хосте (file).</summary>
    public const string Files = "files";

    /// <summary>Первая строка файла — заголовки; "false" отключает (file).</summary>
    public const string HasHeaders = "hasHeaders";

    /// <summary>Разделитель CSV: "," ";" "tab" "|", пусто — определить по первой строке (file).</summary>
    public const string Delimiter = "delimiter";

    /// <summary>В настройке файлы перечислены через ';', в форме — по одному в строке.</summary>
    public static readonly char[] FilesSeparators = [';', '\n', '\r'];

    public static IReadOnlyList<string> ParseFiles(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value
                .Split(FilesSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
}
