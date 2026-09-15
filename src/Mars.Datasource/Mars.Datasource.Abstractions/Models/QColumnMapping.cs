namespace Mars.Datasource.Abstractions.Models;

/// <summary>Преобразование имени типа провайдера в CLR-тип и признак JSON.</summary>
public static class QColumnMapping
{
    public static bool IsJson(string? dataTypeName)
        => dataTypeName?.ToLowerInvariant() is "json" or "jsonb";

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
