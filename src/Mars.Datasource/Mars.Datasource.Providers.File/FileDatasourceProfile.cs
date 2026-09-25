using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Providers.File;

/// <summary>Профиль файлового источника: язык запроса, подсказки и поля формы настроек.</summary>
public static class FileDatasourceProfile
{
    public static DatasourceKindProfile Create() => new()
    {
        Kind = DatasourceKind.File,
        KindLabel = "Файлы — CSV, XLSX",
        Label = "CSV / XLSX",
        Description = "Табличные файлы из медиа-хранилища или с диска хоста; запрос — условие Dynamic LINQ",
        HelpLink = "https://mdimai666.github.io/Mars/",
        DefaultLanguage = DatasourceLanguage.Linq,
        EditorLanguage = DatasourceEditorLanguage.CSharp,
        Hint = """
             Выберите файл слева — откроются его строки.

             Условие фильтра (Dynamic LINQ): Val.Num(age) > 30, name == "ann",
             Val.Date(created) > Val.Date("2020-01-01"). Пустое условие — все строки.
             """,
        EmptyRequestMessage = "Введите условие фильтра или оставьте пустым",
        Features = [DatasourceFeature.Query, DatasourceFeature.Browse],
        Settings =
        [
            new()
            {
                Key = DatasourceSettings.Files,
                Label = "Файлы источника",
                Editor = DatasourceSettingEditor.Lines,
                Placeholder = "2026/09/sales.csv",
                Hint = "По одному в строке: путь в медиа-хранилище или абсолютный путь на хосте",
            },
            new()
            {
                Key = DatasourceSettings.HasHeaders,
                Label = "Первая строка — заголовки",
                Editor = DatasourceSettingEditor.Checkbox,
                Columns = 6,
            },
            new()
            {
                Key = DatasourceSettings.Delimiter,
                Label = "Разделитель CSV",
                Editor = DatasourceSettingEditor.Text,
                Placeholder = ", ; tab |",
                Hint = "Пусто — определить по первой строке",
                Columns = 6,
            },
        ],
    };
}
