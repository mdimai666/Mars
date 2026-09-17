namespace Mars.Datasource.Contracts.Models;

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

    /// <summary>Адрес описания API по умолчанию, если в настройке он не задан.</summary>
    public static string DefaultUrl(string? discovery) => discovery switch
    {
        WordPress => "/wp-json/wp/v2",
        OpenApi => "/swagger/v1/swagger.json",
        _ => "",
    };
}

/// <summary>
/// Способ доступа rest-источника: значения настройки <see cref="DatasourceSettings.AuthMode"/>.
/// Стратегии выполняет <c>Mars.HttpSmartAuthFlow</c>.
/// </summary>
public static class RestAuthMode
{
    public const string None = "";
    public const string Basic = "basic";
    public const string Bearer = "bearer";
    public const string ApiKey = "apiKey";
    public const string CookieForm = "cookieForm";

    public static readonly IReadOnlyList<string> All = [None, Basic, Bearer, ApiKey, CookieForm];
}
