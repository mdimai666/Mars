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

    /// <summary>Адрес API, относительно него разрешаются пути операций (rest).</summary>
    public const string BaseUrl = "baseUrl";

    /// <summary>Как собрать каталог операций: значение <see cref="RestDiscovery"/> (rest).</summary>
    public const string Discovery = "discovery";

    /// <summary>Адрес, откуда брать описание API; пусто — выбрать по типу discovery (rest).</summary>
    public const string DiscoveryUrl = "discoveryUrl";

    /// <summary>Таймаут запроса в секундах; пусто — 100 (rest).</summary>
    public const string TimeoutSec = "timeoutSec";

    /// <summary>Способ доступа: значение <see cref="RestAuthMode"/>; пусто — без доступа (rest).</summary>
    public const string AuthMode = "authMode";

    public const string AuthUsername = "authUsername";
    public const string AuthPassword = "authPassword";
    public const string AuthTokenUrl = "authTokenUrl";
    public const string AuthClientId = "authClientId";
    public const string AuthClientSecret = "authClientSecret";
    public const string AuthScope = "authScope";
    public const string AuthApiKey = "authApiKey";
    public const string AuthApiKeyHeader = "authApiKeyHeader";

    /// <summary>Страница формы входа для cookie-доступа (rest).</summary>
    public const string AuthLoginPageUrl = "authLoginPageUrl";

    /// <summary>Имя файла с запросами пользователя внутри каталога источника.</summary>
    public const string RequestsDocument = "requests.http";

    /// <summary>Имя файла с каталогом discovery внутри каталога источника.</summary>
    public const string CatalogDocument = "catalog.json";

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
