namespace Mars.Datasource.Contracts.Config;

/// <summary>
/// Как rest-источник собирает каталог операций: значения настройки <see cref="DatasourceSettings.Discovery"/>.
/// </summary>
public static class RestDiscovery
{
    /// <summary>Индекс REST API WordPress: <c>routes → endpoints → args</c>.</summary>
    public const string WordPress = "wordpress";

    /// <summary>Документ OpenAPI (swagger.json).</summary>
    public const string OpenApi = "openapi";

    /// <summary>Каталог не собирать: в дереве только запросы пользователя из документа.</summary>
    public const string None = "none";

    public static readonly IReadOnlyList<string> All = [WordPress, OpenApi, None];

    /// <summary>Подпись способа в форме настроек.</summary>
    public static string Label(string? discovery) => discovery switch
    {
        WordPress => "индекс WordPress (wp-json)",
        OpenApi => "документ OpenAPI",
        None => "не собирать — только мои запросы",
        _ => discovery ?? "",
    };

    /// <summary>Адрес описания API по умолчанию, если в настройке он не задан.</summary>
    public static string DefaultUrl(string? discovery) => discovery switch
    {
        WordPress => "/wp-json/wp/v2",
        OpenApi => "/swagger/v1/swagger.json",
        _ => "",
    };
}
