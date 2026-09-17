using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Front.Services;

/// <summary>
/// Чтение и запись маленьких настроек источника (`Settings`) и подписи для формы.
/// Пустое значение в словарь не пишем: настройка без значения означает «по умолчанию».
/// </summary>
public static class DatasourceSettingsEditor
{
    public static string Setting(DatasourceConfig config, string key)
        => config.Settings.TryGetValue(key, out var value) ? value : "";

    public static void SetSetting(DatasourceConfig config, string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) config.Settings.Remove(key);
        else config.Settings[key] = value;
    }

    public static bool HasHeaders(DatasourceConfig config)
        => !string.Equals(Setting(config, DatasourceSettings.HasHeaders), "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>В настройке файлы лежат через ';', в форме удобнее по одному в строке.</summary>
    public static string FilesText(DatasourceConfig config)
        => string.Join("\n", DatasourceSettings.ParseFiles(Setting(config, DatasourceSettings.Files)));

    public static void SetFiles(DatasourceConfig config, string? value)
        => SetSetting(config, DatasourceSettings.Files, string.Join(";", DatasourceSettings.ParseFiles(value)));

    public static string KindLabel(string kind) => kind switch
    {
        DatasourceKind.Sql => "SQL — база данных",
        DatasourceKind.File => "Файлы — CSV, XLSX",
        DatasourceKind.Rest => "REST API — WordPress, OpenAPI",
        _ => kind,
    };

    public static string DiscoveryLabel(string mode) => mode switch
    {
        RestDiscovery.WordPress => "индекс WordPress (wp-json)",
        RestDiscovery.OpenApi => "документ OpenAPI",
        RestDiscovery.None => "не собирать — только мои запросы",
        _ => mode,
    };

    public static string AuthLabel(string mode) => mode switch
    {
        RestAuthMode.None => "без доступа",
        RestAuthMode.Basic => "Basic — логин и пароль",
        RestAuthMode.Bearer => "Bearer — токен по логину",
        RestAuthMode.ApiKey => "API key — заголовок с ключом",
        RestAuthMode.CookieForm => "Cookie — вход через форму",
        _ => mode,
    };

    public static bool UsesAuthUser(string mode) => mode is RestAuthMode.Basic or RestAuthMode.Bearer or RestAuthMode.CookieForm;

    public static bool IsBearerAuth(string mode) => mode == RestAuthMode.Bearer;

    public static bool IsApiKeyAuth(string mode) => mode == RestAuthMode.ApiKey;

    public static bool IsCookieAuth(string mode) => mode == RestAuthMode.CookieForm;
}
