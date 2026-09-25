using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Components;

/// <summary>
/// Поля настроек источника по дескрипторам из профиля провайдера
/// (<see cref="DatasourceKindProfile.Settings"/>): новый тип источника приносит свои поля данными,
/// а не razor-компонентом.
/// </summary>
public partial class DatasourceSettingFields
{
    [Parameter, EditorRequired] public DatasourceConfig Config { get; set; } = default!;

    [Parameter] public IReadOnlyList<DatasourceSettingField> Fields { get; set; } = [];

    /// <summary>Ширина поля в сетке формы; 0 — на всю ширину.</summary>
    static string ColumnClass(DatasourceSettingField setting)
        => setting.Columns is > 0 and < 13 ? $"col-12 col-lg-{setting.Columns} mt-3" : "col-12 mt-3";

    static List<string> Values(DatasourceSettingField setting)
        => setting.Options.Select(option => option.Value).ToList();

    static string OptionLabel(DatasourceSettingField setting, string value)
        => setting.Options.FirstOrDefault(option => option.Value == value)?.Label ?? value;

    string Value(DatasourceSettingField setting) => DatasourceSettingsEditor.Setting(Config, setting.Key);

    void Set(string key, string? value) => DatasourceSettingsEditor.SetSetting(Config, key, value);

    void SetLines(string key, string? value) => DatasourceSettingsEditor.SetLines(Config, key, value);
}
