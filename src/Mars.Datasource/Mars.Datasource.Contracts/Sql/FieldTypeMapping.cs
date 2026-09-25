namespace Mars.Datasource.Contracts.Sql;

/// <summary>Категория типа поля: типы одной категории в UI показываются одним цветом.</summary>
public enum FieldKind
{
    /// <summary>Строки — сюда же попадают незнакомые провайдерские типы (значение у нас всё равно строка).</summary>
    Text,
    Number,
    Boolean,
    DateTime,
    Json,
    Guid,
}

/// <summary>
/// Преобразование имени типа провайдера в CLR-тип и признак JSON. Работает для любого типа источника
/// (колонка таблицы, поле JSON, столбец файла) — это общий словарь, а не часть SQL-мира.
/// </summary>
public static class FieldTypeMapping
{
    public static bool IsJson(string? dataTypeName)
        => dataTypeName?.ToLowerInvariant() is "json" or "jsonb";

    /// <summary>
    /// Короткое имя типа для UI: длинные провайдерские имена («timestamp with time zone») не влезают
    /// в заголовок колонки и раздувают таблицу. Незнакомые имена отдаются как есть (в своём регистре).
    /// </summary>
    public static string ShortTypeName(string? dataTypeName)
    {
        if (string.IsNullOrWhiteSpace(dataTypeName)) return "";

        var name = dataTypeName.Trim();
        var isArray = name.EndsWith("[]", StringComparison.Ordinal);

        if (isArray) name = name[..^2].Trim();

        var shortName = name.ToLowerInvariant() switch
        {
            "timestamp with time zone" => "timestamptz",
            "timestamp without time zone" => "timestamp",
            "time with time zone" => "timetz",
            "time without time zone" => "time",
            "character varying" => "varchar",
            "character" => "char",
            "double precision" => "float8",
            _ => name,
        };

        return isArray ? shortName + "[]" : shortName;
    }

    /// <summary>
    /// Категория типа по имени типа провайдера: это визуальная группа, а не точная типизация —
    /// у незнакомого типа ClrType отдаёт string, поэтому такой тип показывается как строковый.
    /// </summary>
    public static FieldKind Kind(string? dataTypeName)
    {
        if (IsJson(dataTypeName)) return FieldKind.Json;

        var type = ClrType(dataTypeName);

        if (type == typeof(string) || type == typeof(byte[])) return FieldKind.Text;
        if (type == typeof(Guid)) return FieldKind.Guid;
        if (type == typeof(bool)) return FieldKind.Boolean;

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly))
        {
            return FieldKind.DateTime;
        }

        if (type == typeof(short) || type == typeof(int) || type == typeof(long)
            || type == typeof(decimal) || type == typeof(float) || type == typeof(double))
        {
            return FieldKind.Number;
        }

        return FieldKind.Text;
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
