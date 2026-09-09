using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Канонические кодировки значений формы (wire = JSON) и конверсия в CLR — единственное
/// место, где зафиксированы форматы. Decimal уходит строкой в инвариантной культуре
/// (числом JS теряет точность), DateTime — ISO-8601 со смещением, Select — ключом варианта,
/// Relation/File/Image — строкой-Guid, множественные поля — массивом, где порядок равен индексу.
/// Отсутствие ключа в мешке означает «значение не задано»; пустая строка не равна отсутствию.
/// </summary>
public static class FormValueCodec
{
    /// <summary>CLR-значение → канонический JSON по типу поля</summary>
    public static JsonNode? FromClr(object? value, FormFieldType type)
    {
        if (value is null) return null;
        if (type == FormFieldType.Computed) return null;

        return type switch
        {
            FormFieldType.String or FormFieldType.Text or FormFieldType.Select
                => JsonValue.Create(AsString(value)),
            FormFieldType.Bool
                => JsonValue.Create(ReadBool(value)),
            FormFieldType.Int or FormFieldType.Long
                => JsonValue.Create(Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            FormFieldType.Float
                => JsonValue.Create(Convert.ToDouble(value, CultureInfo.InvariantCulture)),
            FormFieldType.Decimal
                => JsonValue.Create(Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)),
            FormFieldType.DateTime
                => JsonValue.Create(AsDate(value).ToString("O", CultureInfo.InvariantCulture)),
            FormFieldType.Relation or FormFieldType.File or FormFieldType.Image
                => JsonValue.Create(AsGuid(value).ToString("D")),
            FormFieldType.SelectMany
                => AsStringArray(value),
            FormFieldType.Object
                => value is JsonNode node ? node.DeepClone() : JsonSerializer.SerializeToNode(value),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    /// <summary>Список CLR-значений → канонический JSON-массив множественного поля (порядок = индекс)</summary>
    public static JsonNode? FromClrList(IEnumerable<object?>? values, FormFieldType type)
    {
        if (values is null) return null;

        var array = new JsonArray();
        foreach (var value in values)
            array.Add(FromClr(value, type));

        return array;
    }

    /// <summary>Канонический JSON → CLR-значение по типу поля</summary>
    public static bool TryToClr(JsonNode? node, FormFieldType type, out object? value, out string? error)
    {
        value = null;
        error = null;

        if (node is null) return true;

        if (type == FormFieldType.Computed)
        {
            error = "вычислимое поле не принимает значение";
            return false;
        }

        switch (type)
        {
            case FormFieldType.String:
            case FormFieldType.Text:
            case FormFieldType.Select:
                if (!TryReadString(node, out var text, out error)) return false;
                value = text;
                return true;

            case FormFieldType.Bool:
                if (!TryReadBool(node, out var flag, out error)) return false;
                value = flag;
                return true;

            case FormFieldType.Int:
            case FormFieldType.Long:
                if (!TryReadLong(node, out var number, out error)) return false;
                if (type == FormFieldType.Int && number is < int.MinValue or > int.MaxValue)
                {
                    error = "число вне диапазона Int32";
                    return false;
                }
                if (type == FormFieldType.Int)
                {
                    value = (int)number;
                    return true;
                }
                value = number;
                return true;

            case FormFieldType.Float:
                if (!TryReadDouble(node, out var floating, out error)) return false;
                value = floating;
                return true;

            case FormFieldType.Decimal:
                if (!TryReadDecimal(node, out var money, out error)) return false;
                value = money;
                return true;

            case FormFieldType.DateTime:
                if (!TryReadDate(node, out var date, out error)) return false;
                value = date;
                return true;

            case FormFieldType.Relation:
            case FormFieldType.File:
            case FormFieldType.Image:
                if (!TryReadGuid(node, out var reference, out error)) return false;
                value = reference;
                return true;

            case FormFieldType.SelectMany:
                if (node is not JsonArray choices)
                {
                    error = "ожидается массив ключей вариантов";
                    return false;
                }
                var keys = new List<string>();
                for (var i = 0; i < choices.Count; i++)
                {
                    if (choices[i] is null) continue;
                    if (!TryReadString(choices[i]!, out var key, out error))
                    {
                        error = $"элемент {i}: {error}";
                        return false;
                    }
                    keys.Add(key);
                }
                value = keys.ToArray();
                return true;

            case FormFieldType.Object:
                value = node;
                return true;

            default:
                error = $"неподдерживаемый тип поля '{type}'";
                return false;
        }
    }

    /// <summary>Канонический JSON множественного поля → список CLR-значений (порядок = индекс)</summary>
    public static bool TryToClrList(JsonNode? node, FormFieldType type, out IReadOnlyList<object?> values, out string? error)
    {
        values = [];
        error = null;

        if (node is null) return true;
        if (node is not JsonArray array)
        {
            error = "ожидается массив значений";
            return false;
        }

        var list = new List<object?>(array.Count);
        for (var i = 0; i < array.Count; i++)
        {
            if (!TryToClr(array[i], type, out var item, out error))
            {
                error = $"элемент {i}: {error}";
                return false;
            }
            list.Add(item);
        }

        values = list;
        return true;
    }

    /// <summary>Форма значения соответствует дескриптору (проверка до правил валидации)</summary>
    public static bool IsShapeValid(FormFieldDescriptor descriptor, JsonNode? node, out string? error)
    {
        error = null;
        if (descriptor.Type == FormFieldType.Computed) return true;

        if (descriptor.Multiple)
            return TryToClrList(node, descriptor.Type, out _, out error);

        if (node is JsonArray)
        {
            error = "поле не множественное: ожидается одно значение";
            return false;
        }

        return TryToClr(node, descriptor.Type, out _, out error);
    }

    /// <summary>Значение пусто: отсутствует, null, пустая строка или пустой массив</summary>
    public static bool IsEmpty(JsonNode? node)
        => node is null
           || node is JsonArray { Count: 0 }
           || (node is JsonValue value && value.TryGetValue<string>(out var text) && text.Length == 0);

    //=====================================

    static bool TryReadString(JsonNode node, out string value, out string? error)
    {
        error = null;
        if (node is JsonValue json)
        {
            if (json.TryGetValue<string>(out var text))
            {
                value = text;
                return true;
            }
            if (json.TryGetValue<Guid>(out var reference))
            {
                value = reference.ToString("D");
                return true;
            }
            if (json.TryGetValue<DateTimeOffset>(out var offset))
            {
                value = offset.ToString("O", CultureInfo.InvariantCulture);
                return true;
            }
            if (json.TryGetValue<DateTime>(out var date))
            {
                value = AsDate(date).ToString("O", CultureInfo.InvariantCulture);
                return true;
            }

            // число/булево любой ширины: JsonValue бывает как разобранным json, так и созданным из CLR-значения
            var raw = json.ToJsonString();
            if (raw.Length > 0 && raw[0] is not '{' and not '[' and not '"')
            {
                value = raw;
                return true;
            }
        }

        value = "";
        error = "ожидается строковое значение";
        return false;
    }

    static bool TryReadBool(JsonNode node, out bool value, out string? error)
    {
        error = null;
        value = false;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<bool>(out var flag))
            {
                value = flag;
                return true;
            }
            if (json.TryGetValue<string>(out var text))
            {
                if (bool.TryParse(text, out var parsed))
                {
                    value = parsed;
                    return true;
                }
            }
            else if (TryReadRawDecimal(json, out var number) && number is 0 or 1)
            {
                value = number == 1;
                return true;
            }
        }

        error = "ожидается логическое значение";
        return false;
    }

    static bool TryReadLong(JsonNode node, out long value, out string? error)
    {
        error = null;
        value = 0;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<long>(out var number))
            {
                value = number;
                return true;
            }
            if (json.TryGetValue<decimal>(out var money))
            {
                if (money % 1 != 0)
                {
                    error = "ожидается целое число";
                    return false;
                }
                value = (long)money;
                return true;
            }
            if (json.TryGetValue<double>(out var floating))
            {
                if (Math.Abs(floating % 1) > double.Epsilon)
                {
                    error = "ожидается целое число";
                    return false;
                }
                value = (long)floating;
                return true;
            }
            if (json.TryGetValue<string>(out var text)
                && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
            if (json.TryGetValue<bool>(out var flag))
            {
                value = flag ? 1 : 0;
                return true;
            }
            if (TryReadRawDecimal(json, out var raw))
            {
                if (raw % 1 != 0)
                {
                    error = "ожидается целое число";
                    return false;
                }
                value = (long)raw;
                return true;
            }
        }

        error ??= "ожидается целое число";
        return false;
    }

    static bool TryReadDouble(JsonNode node, out double value, out string? error)
    {
        error = null;
        value = 0;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<double>(out var floating))
            {
                value = floating;
                return true;
            }
            if (json.TryGetValue<decimal>(out var money))
            {
                value = (double)money;
                return true;
            }
            if (json.TryGetValue<string>(out var text)
                && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
            if (TryReadRawDecimal(json, out var raw))
            {
                value = (double)raw;
                return true;
            }
        }

        error = "ожидается число";
        return false;
    }

    static bool TryReadDecimal(JsonNode node, out decimal value, out string? error)
    {
        error = null;
        value = 0;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<decimal>(out var money))
            {
                value = money;
                return true;
            }
            if (json.TryGetValue<string>(out var text)
                && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
            if (TryReadRawDecimal(json, out value)) return true;
        }

        error = "ожидается число (decimal передаётся строкой)";
        return false;
    }

    /// <summary>
    /// Число из json-представления значения: <see cref="JsonValue"/> может быть как разобранным json
    /// (JsonElement), так и созданным из CLR-значения произвольной числовой ширины.
    /// </summary>
    static bool TryReadRawDecimal(JsonValue json, out decimal value)
    {
        value = 0;
        if (json.TryGetValue<string>(out _)) return false;

        var raw = json.ToJsonString();
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    static bool TryReadDate(JsonNode node, out DateTimeOffset value, out string? error)
    {
        error = null;
        value = default;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<DateTimeOffset>(out var offset))
            {
                value = offset;
                return true;
            }
            if (json.TryGetValue<DateTime>(out var date))
            {
                value = AsDate(date);
                return true;
            }
            if (json.TryGetValue<string>(out var text)
                && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        error = "ожидается дата в формате ISO-8601";
        return false;
    }

    static bool TryReadGuid(JsonNode node, out Guid value, out string? error)
    {
        error = null;
        value = Guid.Empty;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<Guid>(out var reference))
            {
                value = reference;
                return true;
            }
            if (json.TryGetValue<string>(out var text) && Guid.TryParse(text, out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        error = "ожидается идентификатор (Guid)";
        return false;
    }

    static string AsString(object value)
        => value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

    static bool ReadBool(object value)
        => value is bool flag ? flag : Convert.ToBoolean(value, CultureInfo.InvariantCulture);

    static DateTimeOffset AsDate(object value) => value switch
    {
        DateTimeOffset offset => offset,
        DateTime date => AsDate(date),
        string text when DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var parsed) => parsed,
        _ => AsDate(Convert.ToDateTime(value, CultureInfo.InvariantCulture)),
    };

    static DateTimeOffset AsDate(DateTime date) => date.Kind switch
    {
        DateTimeKind.Local => new DateTimeOffset(date),
        _ => new DateTimeOffset(date, TimeSpan.Zero),
    };

    static Guid AsGuid(object value) => value switch
    {
        Guid reference => reference,
        string text when Guid.TryParse(text, out var parsed) => parsed,
        _ => throw new FormatException("не удалось прочитать идентификатор"),
    };

    static JsonArray AsStringArray(object value)
    {
        var array = new JsonArray();
        switch (value)
        {
            case string text:
                array.Add(text);
                break;
            case IEnumerable items:
                foreach (var item in items)
                    array.Add(item is null ? null : JsonValue.Create(AsString(item)));
                break;
            default:
                array.Add(AsString(value));
                break;
        }

        return array;
    }
}
