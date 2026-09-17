using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Components;

/// <summary>Настройки REST-источника: адрес, сборка каталога операций и доступ.</summary>
public partial class RestSourceForm : ComponentBase
{
    [Parameter, EditorRequired] public DatasourceConfig Config { get; set; } = default!;

    string AuthMode => Setting(DatasourceSettings.AuthMode);

    /// <summary>Без адреса API rest-источник не может ни запрос выполнить, ни собрать каталог.</summary>
    string? BaseUrlError => string.IsNullOrWhiteSpace(Setting(DatasourceSettings.BaseUrl))
        ? "Укажите адрес API"
        : null;

    string Setting(string key) => DatasourceSettingsEditor.Setting(Config, key);

    void SetSetting(string key, string? value) => DatasourceSettingsEditor.SetSetting(Config, key, value);
}
