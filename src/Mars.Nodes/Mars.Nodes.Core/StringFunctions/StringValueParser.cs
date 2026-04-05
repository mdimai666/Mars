using System.Globalization;

namespace Mars.Nodes.Core.StringFunctions;

public static class StringValueParser
{
    public static TypeCode TypeToTypeCode(Type type)
    {
        if (type == typeof(bool)) return TypeCode.Boolean;
        if (type == typeof(byte)) return TypeCode.Byte;
        if (type == typeof(char)) return TypeCode.Char;
        if (type == typeof(DateTime)) return TypeCode.DateTime;
        if (type == typeof(decimal)) return TypeCode.Decimal;
        if (type == typeof(double)) return TypeCode.Double;
        if (type == typeof(short)) return TypeCode.Int16;
        if (type == typeof(int)) return TypeCode.Int32;
        if (type == typeof(long)) return TypeCode.Int64;
        if (type == typeof(sbyte)) return TypeCode.SByte;
        if (type == typeof(float)) return TypeCode.Single;
        if (type == typeof(string)) return TypeCode.String;
        if (type == typeof(ushort)) return TypeCode.UInt16;
        if (type == typeof(uint)) return TypeCode.UInt32;
        if (type == typeof(ulong)) return TypeCode.UInt64;
        if (type == typeof(object)) return TypeCode.Object;

        // Для Nullable<T> возвращаем TypeCode для базового типа
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return TypeToTypeCode(Nullable.GetUnderlyingType(type)!);
        }

        // Для массивов возвращаем TypeCode.Object
        if (type.IsArray) return TypeCode.Object;

        // Для остальных случаев возвращаем TypeCode.Object
        return TypeCode.Object;
    }

    /// <summary>
    /// Пытается распарсить строковое значение в указанный тип
    /// </summary>
    /// <param name="typeCode">TypeCode целевого типа</param>
    /// <param name="value">Строковое значение для парсинга</param>
    /// <returns>Распарсенный объект или значение по умолчанию</returns>
    public static object ParseByTypeCode(TypeCode typeCode, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return GetDefaultValue(typeCode);
        }

        return typeCode switch
        {
            TypeCode.String => value,
            TypeCode.Boolean => bool.Parse(value),
            TypeCode.Byte => byte.Parse(value),
            TypeCode.SByte => sbyte.Parse(value),
            TypeCode.Char => char.Parse(value),
            TypeCode.Int16 => short.Parse(value),
            TypeCode.UInt16 => ushort.Parse(value),
            TypeCode.Int32 => int.Parse(value),
            TypeCode.UInt32 => uint.Parse(value),
            TypeCode.Int64 => long.Parse(value),
            TypeCode.UInt64 => ulong.Parse(value),
            TypeCode.Single => float.Parse(value, CultureInfo.InvariantCulture),
            TypeCode.Double => double.Parse(value, CultureInfo.InvariantCulture),
            TypeCode.Decimal => decimal.Parse(value, CultureInfo.InvariantCulture),
            TypeCode.DateTime => DateTime.Parse(value, CultureInfo.InvariantCulture),
            TypeCode.DBNull => DBNull.Value,
            TypeCode.Empty => null!,
            _ => value // По умолчанию возвращаем строку
        };
    }

    /// <summary>
    /// Возвращает значение по умолчанию для TypeCode
    /// </summary>
    public static object GetDefaultValue(TypeCode typeCode)
    {
        return typeCode switch
        {
            TypeCode.String => string.Empty,
            TypeCode.Boolean => false,
            TypeCode.Byte => (byte)0,
            TypeCode.SByte => (sbyte)0,
            TypeCode.Char => '\0',
            TypeCode.Int16 => (short)0,
            TypeCode.UInt16 => (ushort)0,
            TypeCode.Int32 => 0,
            TypeCode.UInt32 => 0u,
            TypeCode.Int64 => 0L,
            TypeCode.UInt64 => 0ul,
            TypeCode.Single => 0f,
            TypeCode.Double => 0d,
            TypeCode.Decimal => 0m,
            TypeCode.DateTime => DateTime.MinValue,
            TypeCode.DBNull => DBNull.Value,
            TypeCode.Empty => null!,
            _ => null!
        };
    }

    /// <summary>
    /// Парсит строковое значение в объект указанного типа (ограниченный набор типов)
    /// </summary>
    /// <param name="type">Целевой тип</param>
    /// <param name="value">Строковое значение для парсинга</param>
    /// <returns>Распарсенный объект или значение по умолчанию</returns>
    public static object ParseByType(Type type, string value)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (string.IsNullOrEmpty(value))
        {
            return GetDefaultValue(type);
        }

        // Для Nullable<T> возвращаем TypeCode для базового типа
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return ParseByType(Nullable.GetUnderlyingType(type)!, value);
        }

        // String
        if (type == typeof(string))
            return value;

        // Boolean
        if (type == typeof(bool))
            return bool.Parse(value);

        // Byte
        if (type == typeof(byte))
            return byte.Parse(value);

        // SByte
        if (type == typeof(sbyte))
            return sbyte.Parse(value);

        // Char
        if (type == typeof(char))
            return char.Parse(value);

        // Int16
        if (type == typeof(short))
            return short.Parse(value);

        // UInt16
        if (type == typeof(ushort))
            return ushort.Parse(value);

        // Int32
        if (type == typeof(int))
            return int.Parse(value);

        // UInt32
        if (type == typeof(uint))
            return uint.Parse(value);

        // Int64
        if (type == typeof(long))
            return long.Parse(value);

        // UInt64
        if (type == typeof(ulong))
            return ulong.Parse(value);

        // Single
        if (type == typeof(float))
            return float.Parse(value, CultureInfo.InvariantCulture);

        // Double
        if (type == typeof(double))
            return double.Parse(value, CultureInfo.InvariantCulture);

        // Decimal
        if (type == typeof(decimal))
            return decimal.Parse(value, CultureInfo.InvariantCulture);

        // DateTime
        if (type == typeof(DateTime))
            return DateTime.Parse(value, CultureInfo.InvariantCulture);

        // DBNull
        if (type == typeof(DBNull))
            return DBNull.Value;

        if (type.IsEnum)
            return Enum.Parse(type, value);

        // Если тип не поддерживается, возвращаем строку
        return value;
    }

    /// <summary>
    /// Пытается распарсить строковое значение в указанный тип
    /// </summary>
    public static bool TryParseByType(Type type, string value, out object result)
    {
        try
        {
            result = ParseByType(type, value);
            return true;
        }
        catch
        {
            result = GetDefaultValue(type);
            return false;
        }
    }

    public static object TryParseByTypeOrDefault(Type type, string value)
    {
        try
        {
            return ParseByType(type, value);
        }
        catch
        {
            return GetDefaultValue(type);
        }
    }

    private static readonly Dictionary<Type, object> DefaultValues = new()
    {
        [typeof(string)] = string.Empty,
        [typeof(bool)] = false,
        [typeof(byte)] = (byte)0,
        [typeof(sbyte)] = (sbyte)0,
        [typeof(char)] = '\0',
        [typeof(short)] = (short)0,
        [typeof(ushort)] = (ushort)0,
        [typeof(int)] = 0,
        [typeof(uint)] = 0u,
        [typeof(long)] = 0L,
        [typeof(ulong)] = 0ul,
        [typeof(float)] = 0f,
        [typeof(double)] = 0d,
        [typeof(decimal)] = 0m,
        [typeof(DateTime)] = DateTime.MinValue,
        [typeof(DBNull)] = DBNull.Value
    };

    /// <summary>
    /// Возвращает значение по умолчанию для типа
    /// </summary>
    public static object GetDefaultValue(Type type)
    {
        return DefaultValues.TryGetValue(type, out var value) ? value : null!;
    }

    /// <summary>
    /// Проверяет, поддерживается ли тип парсером
    /// </summary>
    public static bool IsSupportedType(Type type)
    {
        return type == typeof(string) ||
               type == typeof(bool) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(char) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DBNull);
    }
}
