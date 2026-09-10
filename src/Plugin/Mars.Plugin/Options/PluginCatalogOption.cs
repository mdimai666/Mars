namespace Mars.Plugin.Options;

/// <summary>
/// Подключение каталога плагинов (витрины маркетплейса). Задаётся в
/// <c>appsettings.json</c> секцией <see cref="SectionName"/>; из админки не
/// редактируется (в отличие от <c>PluginManagerSettingsOption</c>).
/// </summary>
public class PluginCatalogOption
{
    public const string SectionName = "PluginCatalog";

    /// <summary>Базовый URL каталога, например <c>https://catalog.mars-dotnet.org</c>.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Включён ли каталог (витрина маркетплейса).</summary>
    public bool Enabled { get; set; }
}
