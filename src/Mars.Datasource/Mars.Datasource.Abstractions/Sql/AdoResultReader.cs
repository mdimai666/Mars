using System.Data.Common;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Abstractions.Sql;

/// <summary>
/// Общие для sql-драйверов преобразования ADO: поле из схемы результата, чтение строк с лимитом,
/// подстановка параметров. Чистое форматирование значений — в <see cref="QueryResultMapping"/>.
/// </summary>
public static class AdoResultReader
{
    public static DatasourceField Field(DbColumn column, bool isKey = false)
        => new()
        {
            Name = column.ColumnName,
            Ordinal = column.ColumnOrdinal ?? 0,
            DataTypeName = column.DataTypeName ?? "",
            ClrTypeName = column.DataType?.FullName ?? "",
            IsNullable = column.AllowDBNull ?? true,
            IsKey = isKey,
            IsJson = FieldTypeMapping.IsJson(column.DataTypeName),
        };

    /// <summary>
    /// Читает строки до <paramref name="maxRows"/> (0 — без ограничения).
    /// Truncated = true означает, что в результате есть ещё строки.
    /// </summary>
    public static async Task<(string?[][] Rows, bool Truncated)> ReadRowsAsync(
        DbDataReader reader,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        List<string?[]> rows = [];

        while (await reader.ReadAsync(cancellationToken))
        {
            if (maxRows > 0 && rows.Count >= maxRows)
            {
                return (rows.ToArray(), true);
            }

            var row = new string?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = QueryResultMapping.Format(reader.GetValue(i));
            }

            rows.Add(row);
        }

        return (rows.ToArray(), false);
    }

    /// <summary>
    /// Подставить параметры. <paramref name="configure"/> нужен провайдерам, которым мало
    /// значения-строки: например Postgres выводит тип параметра из контекста только для unknown.
    /// </summary>
    public static void ApplyParameters(DbCommand command, IReadOnlyList<DatasourceParam>? parameters, Action<DbParameter>? configure = null)
    {
        if (parameters is null) return;

        foreach (var parameter in parameters)
        {
            var dbParameter = CreateParameter(command, parameter);
            configure?.Invoke(dbParameter);
            command.Parameters.Add(dbParameter);
        }
    }

    static DbParameter CreateParameter(DbCommand command, DatasourceParam parameter)
    {
        var dbParameter = command.CreateParameter();
        dbParameter.ParameterName = parameter.Name;
        dbParameter.Value = (object?)parameter.Value ?? DBNull.Value;
        return dbParameter;
    }
}
