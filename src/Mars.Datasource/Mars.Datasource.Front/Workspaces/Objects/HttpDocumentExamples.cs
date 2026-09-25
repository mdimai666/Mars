namespace Mars.Datasource.Front.Workspaces.Objects;

/// <summary>Заготовка запроса для документа .http из меню «примеры».</summary>
public record HttpDocumentExample(string Name, string Text);

/// <summary>
/// Примеры запросов: дописываются в конец документа заготовкой, которую пользователь
/// правит под свой API. Адрес — переменная {{baseUrl}} из настроек rest-источника.
/// </summary>
public static class HttpDocumentExamples
{
    public static IReadOnlyList<HttpDocumentExample> All { get; } =
    [
        new("GET — список с параметрами",
            """
            GET {{baseUrl}}/items?per_page=10&page=1
            Accept: application/json
            """),

        new("GET — элемент по идентификатору",
            """
            GET {{baseUrl}}/items/1
            Accept: application/json
            """),

        new("POST — создание (JSON-тело)",
            """
            POST {{baseUrl}}/items
            Content-Type: application/json

            {
              "title": "Новый элемент",
              "status": "draft"
            }
            """),

        new("PUT — обновление (JSON-тело)",
            """
            PUT {{baseUrl}}/items/1
            Content-Type: application/json

            {
              "title": "Обновлённое название"
            }
            """),

        new("DELETE — удаление",
            """
            DELETE {{baseUrl}}/items/1
            """),

        new("Запрос с переменной документа",
            """
            @itemId = 1

            GET {{baseUrl}}/items/{{itemId}}
            Accept: application/json
            """),
    ];
}
