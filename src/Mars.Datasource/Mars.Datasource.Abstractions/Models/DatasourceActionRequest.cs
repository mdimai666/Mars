namespace Mars.Datasource;

/// <summary>
/// Запрос на выполнение действия источника данных (полезные SQL-запросы, бэкап и т.п.).
/// Собственный тип Datasource — не переиспользует XActions-запрос, чтобы развиваться независимо.
/// </summary>
public class DatasourceActionRequest
{
    public required string ActionId { get; set; }
    public Dictionary<string, string> Arguments { get; set; } = [];
}
