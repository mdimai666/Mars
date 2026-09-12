using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Канонические кодировки значений формы (wire = JSON) и конверсия в CLR — единственное
/// место, где зафиксированы форматы. Decimal уходит строкой в инвариантной культуре
/// (числом JS теряет точность), DateTime — ISO-8601 со смещением строкой, Select — ключом
/// варианта, Relation/File/Image — строкой-Guid, множественные поля — массивом, где порядок
/// равен индексу. Чтение строгое: одна каноническая форма на тип, отклонение — ошибка формата
/// (терпимое «угадывание типа» скрывало бы рассинхрон клиента и сервера).
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
    public static JsonNode? FromClrList(IEnumerable? values, FormFieldType type)
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
        value = "";

        if (node is JsonValue json && json.TryGetValue<string>(out var text))
        {
            value = text;
            return true;
        }

        error = "ожидается строка";
        return false;
    }

    static bool TryReadBool(JsonNode node, out bool value, out string? error)
    {
        error = null;
        value = false;

        if (node is JsonValue json && json.TryGetValue<bool>(out var flag))
        {
            value = flag;
            return true;
        }

        error = "ожидается true или false";
        return false;
    }

    static bool TryReadLong(JsonNode node, out long value, out string? error)
    {
        error = null;
        value = 0;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<long>(out var fromLong))
            {
                value = fromLong;
                return true;
            }

            // CLR-значение могло быть создано узким целым — это тот же json-номер
            if (json.TryGetValue<int>(out var fromInt))
            {
                value = fromInt;
                return true;
            }
        }

        error = "ожидается целое число";
        return false;
    }

    static bool TryReadDouble(JsonNode node, out double value, out string? error)
    {
        error = null;
        value = 0;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<double>(out var fromDouble))
            {
                value = fromDouble;
                return true;
            }

            if (json.TryGetValue<float>(out var fromFloat))
            {
                value = fromFloat;
                return true;
            }
        }

        error = "ожидается число";
        return false;
    }

    /// <summary>Decimal передаётся строкой: числом его читает только CLR, JS точность теряет</summary>
    static bool TryReadDecimal(JsonNode node, out decimal value, out string? error)
    {
        error = null;
        value = 0;

        if (node is JsonValue json
            && json.TryGetValue<string>(out var text)
            && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = "ожидается строка с числом (decimal передаётся строкой)";
        return false;
    }

    /// <summary>Дата передаётся строкой ISO-8601 со смещением</summary>
    static bool TryReadDate(JsonNode node, out DateTimeOffset value, out string? error)
    {
        error = null;
        value = default;

        if (node is JsonValue json
            && json.TryGetValue<string>(out var text)
            && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = "ожидается дата ISO-8601 строкой";
        return false;
    }

    static bool TryReadGuid(JsonNode node, out Guid value, out string? error)
    {
        error = null;
        value = Guid.Empty;

        if (node is JsonValue json && json.TryGetValue<string>(out var text) && Guid.TryParse(text, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = "ожидается строка-Guid";
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
