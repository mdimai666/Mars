using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.HttpSmartAuthFlow;

namespace Mars.Datasource.Front.Services;

/// <summary>
/// Чтение и запись маленьких настроек источника (`Settings`).
/// Пустое значение в словарь не пишем: настройка без значения означает «по умолчанию».
/// </summary>
public static class DatasourceSettingsEditor
{
    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Setting(DatasourceConfig config, string key)
        => config.Settings.TryGetValue(key, out var value) ? value : "";

    public static void SetSetting(DatasourceConfig config, string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) config.Settings.Remove(key);
        else config.Settings[key] = value;
    }

    /// <summary>Флажок: «false» — снят, пустое значение и всё остальное — отмечен.</summary>
    public static bool Flag(DatasourceConfig config, string key)
        => !string.Equals(Setting(config, key), "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>Список значений: в настройке через ';', в форме — по одному в строке.</summary>
    public static string LinesText(DatasourceConfig config, string key)
        => string.Join("\n", DatasourceSettings.ParseFiles(Setting(config, key)));

    public static void SetLines(DatasourceConfig config, string key, string? value)
        => SetSetting(config, key, string.Join(";", DatasourceSettings.ParseFiles(value)));

    /// <summary>
    /// Доступы источника: общий <see cref="AuthConfig"/> одним JSON-значением настройки.
    /// Битый JSON читаем как «без доступа»: источник остаётся рабочим, а поле можно заполнить заново.
    /// </summary>
    public static AuthConfig? Auth(DatasourceConfig config)
    {
        var json = Setting(config, DatasourceSettings.Auth);

        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<AuthConfig>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void SetAuth(DatasourceConfig config, AuthConfig? value)
        => SetSetting(config, DatasourceSettings.Auth, value is null ? null : JsonSerializer.Serialize(value, Json));
}
