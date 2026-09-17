using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Components;

/// <summary>Настройки файлового источника: список файлов, разделитель CSV и строка заголовков.</summary>
public partial class FileSourceForm : ComponentBase
{
    [Parameter, EditorRequired] public DatasourceConfig Config { get; set; } = default!;

    string Setting(string key) => DatasourceSettingsEditor.Setting(Config, key);

    void SetSetting(string key, string? value) => DatasourceSettingsEditor.SetSetting(Config, key, value);
}
