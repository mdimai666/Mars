using System.Text.Json.Serialization;
using Mars.Core.Models;

namespace Mars.Datasource.Contracts.Query;

/// <summary>
/// Результат запроса к источнику: описание полей и строки значений текстом.
/// Значения приводятся к строке обратимо (<see cref="Mars.Datasource.Contracts.Query.QueryResultDto.Data"/>
/// и форматирование провайдера), чтобы отредактированное значение можно было вернуть параметром.
/// </summary>
public class QueryResultDto : IUserActionResult<string[][]?>
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";

    /// <summary>Тип источника и его вариант: значения <see cref="DatasourceKind"/> и ключ драйвера.</summary>
    public string Kind { get; set; } = "";
    public string Driver { get; set; } = "";

    public DatasourceField[] Fields { get; set; } = [];
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
        Fields.Select(item => item.Name).ToArray(),
        .. Rows.Select(row => row.Select(value => value ?? "").ToArray()),
    ];
}
