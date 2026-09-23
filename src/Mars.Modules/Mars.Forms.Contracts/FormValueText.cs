using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Текстовый слой поверх <see cref="FormValueCodec"/>: превращает сырой текст (аргумент
/// ИИ-инструмента, команда CLI, импорт) в значение поля формы. Допускает человеко-понятные
/// форматы там, где wire-формат строгий: bool — «true»/«false» в любом регистре, числа —
/// инвариантная культура, дата — любой распознаваемый ISO-8601, SelectMany и множественные
/// поля — JSON-массив или CSV, пустая строка — «снять значение». Каноническая форма результата
/// не расходится с <see cref="FormValueCodec"/>: парсер лишь готовит JsonNode и передаёт
/// его кодеку, а ключи вариантов Select/SelectMany дополнительно сверяются с
/// <see cref="FormFieldDescriptor.Choices"/>.
/// </summary>
public static class FormValueText
{
    /// <summary>
    /// Текст → значение поля: одиночное — канонический CLR (строка/число/дата/Guid/ключ варианта),
    /// множественное — <see cref="IReadOnlyList{T}"/> (порядок = индекс), SelectMany — массив ключей.
    /// Пустой/пробельный текст — «снять значение» (null; пустой список для множественного).
    /// </summary>
    public static bool TryToClr(string? raw, FormFieldDescriptor field, out object? value, out string? error)
    {
        value = null;
        error = null;

        if (field.Type == FormFieldType.Computed)
        {
            error = "вычислимое поле не принимает значение";
            return false;
        }

        // множественное поле (не SelectMany — у него список живёт в одной строке значения)
        if (field.Multiple && field.Type != FormFieldType.SelectMany)
        {
            if (!TryToListNode(raw, field.Type, out var listNode, out error)) return false;
            if (!FormValueCodec.TryToClrList(listNode, field.Type, out var values, out error)) return false;
            value = values;
            return true;
        }

        if (!TryToNode(raw, field.Type, out var node, out error)) return false;
        if (!FormValueCodec.TryToClr(node, field.Type, out value, out error)) return false;
        return CheckChoices(field, value, out error);
    }

    /// <summary>
    /// Текст → канонический JsonNode одиночного значения (для записи в транспорт/JSON-пути CMS).
    /// Пустой/пробельный текст — null-узел («значение не задано»).
    /// </summary>
    public static bool TryToNode(string? raw, FormFieldType type, out JsonNode? node, out string? error)
    {
        node = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw)) return true;

        switch (type)
        {
            case FormFieldType.String:
            case FormFieldType.Text:
            case FormFieldType.Select:
                node = JsonValue.Create(raw);
                return true;

            case FormFieldType.Bool:
                if (!bool.TryParse(raw.Trim(), out var flag))
                {
                    error = "ожидается 'true' или 'false'";
                    return false;
                }
                node = JsonValue.Create(flag);
                return true;

            case FormFieldType.Int:
            case FormFieldType.Long:
                if (!long.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                {
                    error = "ожидается целое число";
                    return false;
                }
                node = JsonValue.Create(number);
                return true;

            case FormFieldType.Float:
                if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var floating))
                {
                    error = "ожидается число";
                    return false;
                }
                node = JsonValue.Create(floating);
                return true;

            case FormFieldType.Decimal:
                // канонический wire decimal — строка (числом JS теряет точность)
                if (!decimal.TryParse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var money))
                {
                    error = "ожидается число (decimal)";
                    return false;
                }
                node = JsonValue.Create(money.ToString(CultureInfo.InvariantCulture));
                return true;

            case FormFieldType.DateTime:
                if (!DateTimeOffset.TryParse(raw.Trim(), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out var date))
                {
                    error = "ожидается дата ISO-8601";
                    return false;
                }
                node = JsonValue.Create(date.ToString("O", CultureInfo.InvariantCulture));
                return true;

            case FormFieldType.Relation:
            case FormFieldType.File:
            case FormFieldType.Image:
                if (!Guid.TryParse(raw.Trim(), out var reference))
                {
                    error = "ожидается идентификатор объекта (Guid)";
                    return false;
                }
                node = JsonValue.Create(reference.ToString("D"));
                return true;

            case FormFieldType.SelectMany:
                node = ToStringArrayNode(raw);
                return true;

            case FormFieldType.Object:
                try
                {
                    node = JsonNode.Parse(raw);
                    return true;
                }
                catch (JsonException ex)
                {
                    error = "ожидается JSON: " + ex.Message;
                    return false;
                }

            default:
                error = $"неподдерживаемый тип поля '{type}'";
                return false;
        }
    }

    /// <summary>
    /// Текст → JsonArray значений множественного поля: JSON-массив как есть (элементы
    /// в канонической кодировке проверит кодек) либо CSV, где каждая часть — текст
    /// в формате элемента. Строки с запятыми внутри — только JSON-массивом.
    /// Пустой текст — пустой массив («очистить список»).
    /// </summary>
    public static bool TryToListNode(string? raw, FormFieldType elementType, out JsonNode? node, out string? error)
    {
        node = null;
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            node = new JsonArray();
            return true;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                if (JsonNode.Parse(trimmed) is JsonArray parsed)
                {
                    node = parsed;
                    return true;
                }
            }
            catch (JsonException ex)
            {
                error = "ожидается JSON-массив значений: " + ex.Message;
                return false;
            }
        }

        var array = new JsonArray();
        foreach (var part in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryToNode(part, elementType, out var item, out error)) return false;
            array.Add(item?.DeepClone());
        }

        node = array;
        return true;
    }

    //=====================================

    /// <summary>Ключи вариантов Select/SelectMany сверяются с определением поля — иначе значение молча станет пустым</summary>
    static bool CheckChoices(FormFieldDescriptor field, object? value, out string? error)
    {
        error = null;

        if (field.Choices.Count == 0) return true;

        string[] keys = field.Type switch
        {
            FormFieldType.SelectMany => value as string[] ?? [],
            FormFieldType.Select => [value as string ?? ""],
            _ => [],
        };

        foreach (var key in keys)
        {
            if (key.Length == 0) continue;
            if (field.Choices.All(choice => choice.Key != key))
            {
                var available = string.Join(", ", field.Choices.Select(choice => choice.Key));
                error = $"вариант '{key}' не найден; доступные ключи: {available}";
                return false;
            }
        }

        return true;
    }

    /// <summary>Список ключей вариантов: JSON-массив (проверку элементов делает кодек) либо CSV</summary>
    static JsonNode ToStringArrayNode(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                if (JsonNode.Parse(trimmed) is JsonArray parsed) return parsed;
            }
            catch (JsonException)
            {
                // не массив — разбираем как CSV
            }
        }

        var array = new JsonArray();
        foreach (var part in trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            array.Add(part);

        return array;
    }
}
