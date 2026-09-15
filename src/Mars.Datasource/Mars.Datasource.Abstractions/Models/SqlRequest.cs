namespace Mars.Datasource.Abstractions.Models;

/// <summary>
/// Запрос к источнику. Параметры подставляются драйвером как <c>@name</c>.
/// </summary>
public class SqlRequest
{
    public string Sql { get; set; } = "";
    public List<SqlParam>? Parameters { get; set; }

    /// <summary>Максимум строк результата; 0 — без ограничения.</summary>
    public int MaxRows { get; set; }

    /// <summary>Таймаут запроса в секундах; null — провайдерский по умолчанию.</summary>
    public int? TimeoutSec { get; set; }
}

/// <summary>
/// Параметр запроса. <see cref="Value"/> = null означает SQL NULL.
/// </summary>
public class SqlParam
{
    public string Name { get; set; } = "";
    public string? Value { get; set; }
}
