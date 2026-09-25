namespace Mars.Datasource.Contracts.Config;

/// <summary>
/// Поле формы настроек источника. Провайдер описывает свои настройки данными, поэтому форма
/// рисуется общая и новый тип источника не приносит свой razor-компонент.
/// Значения хранятся в <see cref="DatasourceConfig.Settings"/> строкой под ключом <see cref="Key"/>.
/// </summary>
public class DatasourceSettingField
{
    /// <summary>Ключ в <see cref="DatasourceConfig.Settings"/>: значение <see cref="DatasourceSettings"/>.</summary>
    public string Key { get; set; } = "";

    public string Label { get; set; } = "";

    /// <summary>Чем рисовать поле: значение <see cref="DatasourceSettingEditor"/>.</summary>
    public string Editor { get; set; } = DatasourceSettingEditor.Text;

    public string Placeholder { get; set; } = "";

    /// <summary>Пояснение под полем.</summary>
    public string Hint { get; set; } = "";

    /// <summary>Варианты для <see cref="DatasourceSettingEditor.Select"/>.</summary>
    public List<DatasourceSettingOption> Options { get; set; } = [];

    /// <summary>Ширина поля в сетке Bootstrap (1–12); 0 — на всю ширину.</summary>
    public int Columns { get; set; }
}

public class DatasourceSettingOption
{
    public string Value { get; set; } = "";

    public string Label { get; set; } = "";

    public DatasourceSettingOption()
    {
    }

    public DatasourceSettingOption(string value, string label)
    {
        Value = value;
        Label = label;
    }
}

public static class DatasourceSettingEditor
{
    public const string Text = "text";
    public const string Password = "password";
    public const string Number = "number";
    public const string Select = "select";
    public const string Checkbox = "checkbox";

    /// <summary>Многострочный список значений по одному в строке.</summary>
    public const string Lines = "lines";

    /// <summary>Доступы источника: общий редактор <c>AuthConfig</c>.</summary>
    public const string Auth = "auth";
}
