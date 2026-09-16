namespace Mars.Datasource.Contracts.Models;

/// <summary>
/// Ключи <see cref="DatasourceConfig.Settings"/>: по ним форма настроек и провайдер источника
/// договариваются без общих типов. Провайдер может читать и свои ключи, эти — общие для формы.
/// </summary>
public static class DatasourceSettings
{
    /// <summary>Файл источника по умолчанию (file).</summary>
    public const string File = "file";

    /// <summary>Первая строка файла — заголовки; "false" отключает (file).</summary>
    public const string HasHeaders = "hasHeaders";

    /// <summary>Разделитель CSV: "," ";" "tab" "|", пусто — определить по первой строке (file).</summary>
    public const string Delimiter = "delimiter";
}
