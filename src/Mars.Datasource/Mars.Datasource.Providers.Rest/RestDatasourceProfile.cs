using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Providers.Rest;

/// <summary>Профиль rest-источника: язык запроса, подсказки и поля формы настроек.</summary>
public static class RestDatasourceProfile
{
    public static DatasourceKindProfile Create() => new()
    {
        Kind = DatasourceKind.Rest,
        KindLabel = "REST API — WordPress, OpenAPI",
        Label = "WordPress / OpenAPI",
        Description = "HTTP API: каталог операций из описания API и документ запросов .http",
        HelpLink = "https://mdimai666.github.io/Mars/",
        DefaultLanguage = DatasourceLanguage.Http,
        EditorLanguage = DatasourceEditorLanguage.Http,
        OpensAsDocument = true,
        DocumentName = DatasourceSettings.RequestsDocument,
        DefaultGroup = "GET",
        CollapseGroupsAbove = 25,
        Hint = """
             Все запросы источника лежат в документе requests.http: дерево слева разложено по методам
             и только переходит к нужному запросу, клик по операции ничего не выполняет.
             Запрос уходит по ссылке «выполнить» над ним в редакторе или по кнопке «Выполнить блок»
             (курсор внутри запроса).

             Запрос можно написать в редакторе или выбрать операцию в дереве и заполнить её параметры:
             подставленные значения уходят переменными, остальные — в строку запроса (чтение)
             или в JSON-тело (запись).

             Синтаксис .http: GET {{baseUrl}}/wp-json/wp/v2/posts?per_page=10, заголовки через строку,
             тело после пустой строки, разделитель запросов ###, имя запроса # @name posts,
             переменные @var = 1 и {{var}}. Ctrl+S сохраняет документ на сервер.
             """,
        EmptyRequestMessage = "Введите запрос или выберите операцию в дереве",
        Features =
        [
            DatasourceFeature.Query,
            DatasourceFeature.Browse,
            DatasourceFeature.Write,
            DatasourceFeature.Document,
            DatasourceFeature.Discover,
        ],
        Settings =
        [
            new()
            {
                Key = DatasourceSettings.BaseUrl,
                Label = "Адрес API (baseUrl)",
                Placeholder = "https://example.org",
                Hint = "В запросах источника адрес доступен как переменная {{baseUrl}}",
            },
            new()
            {
                Key = DatasourceSettings.Discovery,
                Label = "Каталог операций",
                Editor = DatasourceSettingEditor.Select,
                Columns = 4,
                Options = RestDiscovery.All
                    .Select(mode => new DatasourceSettingOption(mode, RestDiscovery.Label(mode)))
                    .ToList(),
            },
            new()
            {
                Key = DatasourceSettings.DiscoveryUrl,
                Label = "Адрес описания API",
                Placeholder = "/wp-json/wp/v2 или /swagger/v1/swagger.json",
                Hint = "Пусто — взять адрес по умолчанию для выбранного способа",
                Columns = 5,
            },
            new()
            {
                Key = DatasourceSettings.TimeoutSec,
                Label = "Таймаут, секунд",
                Editor = DatasourceSettingEditor.Number,
                Placeholder = "100",
                Columns = 3,
            },
            new()
            {
                Key = DatasourceSettings.Auth,
                Label = "Доступ",
                Editor = DatasourceSettingEditor.Auth,
            },
        ],
    };
}
