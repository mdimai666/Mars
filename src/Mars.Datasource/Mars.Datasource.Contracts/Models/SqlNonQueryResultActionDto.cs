using Mars.Core.Models;

namespace Mars.Datasource.Contracts.Models;

/// <summary>
/// Результат запроса без возврата данных (INSERT/UPDATE/DELETE/DDL) —
/// с числом затронутых строк.
/// </summary>
public class SqlNonQueryResultActionDto : IUserActionResult<int>
{
    public bool Ok { get; set; }
    public string Message { get; set; } = default!;
    public string DatabaseDriver { get; set; } = default!;
    public int RowsAffected { get; set; }
    public int Data => RowsAffected;
}
