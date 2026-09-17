using System.Text.Json.Serialization;
using Mars.Core.Models;

namespace Mars.Datasource.Contracts.Models;

/// <summary>
/// Колонка результата запроса.
/// </summary>
public class QueryColumn
{
    public string Name { get; set; } = "";
    public string DataTypeName { get; set; } = "";
    public string ClrTypeName { get; set; } = "";
    public bool IsNullable { get; set; } = true;
    public bool IsKey { get; set; }
    public bool IsJson { get; set; }
}

/// <summary>
/// Результат SQL-запроса с описанием колонок.
/// </summary>
public class QueryResultDto : IUserActionResult<string[][]?>
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string DatabaseDriver { get; set; } = "";

    public QueryColumn[] Columns { get; set; } = [];
    public string?[][] Rows { get; set; } = [];

    /// <summary>Сработал лимит строк запроса — данные прочитаны не полностью.</summary>
    public bool Truncated { get; set; }
    public long ElapsedMs { get; set; }
    public string Command { get; set; } = "";

    /// <summary>
    /// Ответ документом: есть у источников, чей ответ не раскладывается в таблицу
    /// (объект JSON, текст, XML). Табличные ответы его не заполняют.
    /// </summary>
    public string? Json { get; set; }

    /// <summary>Сколько всего записей у источника, если он это сообщил (WordPress <c>X-WP-Total</c>).</summary>
    public long? Total { get; set; }

    /// <summary>
    /// Проекция в старый формат (первая строка — заголовки). Её читают ноды и AI-инструменты,
    /// поэтому она не уезжает по HTTP и NULL в ней приводится к пустой строке.
    /// </summary>
    [JsonIgnore]
    public string[][] Data =>
    [
        Columns.Select(c => c.Name).ToArray(),
        .. Rows.Select(row => row.Select(v => v ?? "").ToArray()),
    ];
}
