using Mars.Core.Models;

namespace Mars.Datasource.Contracts.Query;

/// <summary>
/// Результат запроса, который не возвращает данные: INSERT/UPDATE/DDL у sql-источника,
/// POST/PUT/DELETE у rest-источника. <see cref="RowsAffected"/> источник сообщает не всегда.
/// </summary>
public class DatasourceModifyResult : IUserActionResult<int>
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";

    /// <summary>Тип источника и его вариант: значения <see cref="DatasourceKind"/> и ключ драйвера.</summary>
    public string Kind { get; set; } = "";
    public string Driver { get; set; } = "";

    public int RowsAffected { get; set; }

    public int Data => RowsAffected;
}
