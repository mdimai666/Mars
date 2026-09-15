using System.Data.Common;
using System.Globalization;

namespace Mars.Datasource.Abstractions.Models;

/// <summary>
/// Общие для провайдеров преобразования результатов запроса — чтобы драйверы не дублировали их.
/// </summary>
public static class QueryResultMapping
{
    public static QueryColumn Column(DbColumn column, bool isKey = false)
        => new()
        {
            Name = column.ColumnName,
            DataTypeName = column.DataTypeName ?? "",
            ClrTypeName = column.DataType?.FullName ?? "",
            IsNullable = column.AllowDBNull ?? true,
            IsKey = isKey,
            IsJson = IsJsonType(column.DataTypeName),
        };

    public static bool IsJsonType(string? dataTypeName)
        => dataTypeName?.ToLowerInvariant() is "json" or "jsonb";

    /// <summary>
    /// Значение в строку. Даты/время — в инвариантном формате, чтобы отредактированное
    /// значение можно было вернуть в базу параметром без потери смысла.
    /// </summary>
    public static string? Format(object? value)
        => value switch
        {
            null or DBNull => null,
            string s => s,
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToBase64String(bytes),
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
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
                row[i] = Format(reader.GetValue(i));
            }

            rows.Add(row);
        }

        return (rows.ToArray(), false);
    }

    /// <summary>Текст ошибки драйвера в одну строку: в UI он показывается целиком.</summary>
    public static string Error(Exception ex)
    {
        var message = (ex.GetBaseException().Message ?? ex.Message).ReplaceLineEndings(" ").Trim();
        return message.Length > 500 ? message[..500] + "…" : message;
    }

    public static void ApplyParameters(DbCommand command, IReadOnlyList<SqlParam>? parameters)
    {
        if (parameters is null) return;

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(CreateParameter(command, parameter));
        }
    }

    static DbParameter CreateParameter(DbCommand command, SqlParam parameter)
    {
        var dbParameter = command.CreateParameter();
        dbParameter.ParameterName = parameter.Name;
        dbParameter.Value = (object?)parameter.Value ?? DBNull.Value;
        return dbParameter;
    }
}
