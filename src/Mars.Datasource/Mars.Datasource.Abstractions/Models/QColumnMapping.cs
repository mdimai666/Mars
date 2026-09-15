namespace Mars.Datasource.Abstractions.Models;

/// <summary>Категория типа колонки: типы одной категории в UI показываются одним цветом.</summary>
public enum QColumnKind
{
    /// <summary>Строки — сюда же попадают незнакомые провайдерские типы (значение у нас всё равно строка).</summary>
    Text,
    Number,
    Boolean,
    DateTime,
    Json,
    Guid,
}

/// <summary>Преобразование имени типа провайдера в CLR-тип и признак JSON.</summary>
public static class QColumnMapping
{
    public static bool IsJson(string? dataTypeName)
        => dataTypeName?.ToLowerInvariant() is "json" or "jsonb";

    /// <summary>
    /// Категория типа по имени типа провайдера: это визуальная группа, а не точная типизация —
    /// у незнакомого типа ClrType отдаёт string, поэтому такой тип показывается как строковый.
    /// </summary>
    public static QColumnKind Kind(string? dataTypeName)
    {
        if (IsJson(dataTypeName)) return QColumnKind.Json;

        var type = ClrType(dataTypeName);

        if (type == typeof(string) || type == typeof(byte[])) return QColumnKind.Text;
        if (type == typeof(Guid)) return QColumnKind.Guid;
        if (type == typeof(bool)) return QColumnKind.Boolean;

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly))
        {
            return QColumnKind.DateTime;
        }

        if (type == typeof(short) || type == typeof(int) || type == typeof(long)
            || type == typeof(decimal) || type == typeof(float) || type == typeof(double))
        {
            return QColumnKind.Number;
        }

        return QColumnKind.Text;
    }

    public static Type ClrType(string? dataTypeName)
        => dataTypeName?.ToLowerInvariant().Trim() switch
        {
            "smallint" or "int2" or "tinyint" => typeof(short),
            "integer" or "int" or "int4" or "mediumint" => typeof(int),
            "bigint" or "int8" => typeof(long),
            "numeric" or "decimal" or "money" or "smallmoney" => typeof(decimal),
            "real" or "float4" => typeof(float),
            "double precision" or "float" or "float8" => typeof(double),
            "boolean" or "bool" or "bit" => typeof(bool),
            "date" => typeof(DateOnly),
            "time" or "time without time zone" => typeof(TimeOnly),
            "uuid" or "uniqueidentifier" => typeof(Guid),
            "bytea" or "varbinary" or "binary" or "image" or "blob" => typeof(byte[]),
            "timestamp" or "timestamp without time zone" or "datetime" or "datetime2" or "smalldatetime" => typeof(DateTime),
            "timestamp with time zone" or "timestamptz" or "datetimeoffset" => typeof(DateTimeOffset),
            _ => typeof(string),
        };
}
