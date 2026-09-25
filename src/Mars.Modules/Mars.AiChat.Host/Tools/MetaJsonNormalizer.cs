using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Contracts.MetaFields;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Терпимый вход ИИ-модели → строгая форма JSON-пути записи мета-значений
/// (<c>PostJsonService</c> → <c>MetaFieldUtils.MetaValueFromJson</c>).
/// Скаляры выдаются узлами wire-JSON (<see cref="JsonNode.Parse(string, JsonNodeOptions?, JsonDocumentOptions)"/>),
/// т.к. CLR-узлы не конвертируются при <c>GetValue&lt;T&gt;</c> (строка→Guid и т.п. падают).
/// Исключение — SelectMany: CLR-узел <c>JsonValue(Guid[])</c>, потому что wire-массив
/// JSON-путь разбирает поэлементно в отдельные строки (ai/AiChatMetaFieldsPlan.md, B5).
/// </summary>
internal static class MetaJsonNormalizer
{
    /// <summary>Текст аргумента инструмента → словарь «ключ поля → значение»; пустой текст → пустой словарь</summary>
    public static bool TryParseObject(string? json, out Dictionary<string, JsonNode?> meta, out string? error)
    {
        meta = [];
        error = null;

        if (string.IsNullOrWhiteSpace(json)) return true;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            error = "metaJson не является JSON: " + ex.Message;
            return false;
        }

        if (node is not JsonObject obj)
        {
            error = "metaJson должен быть JSON-объектом «ключ поля: значение»";
            return false;
        }

        foreach (var pair in obj)
            meta[pair.Key] = pair.Value;

        return true;
    }

    /// <summary>
    /// Нормализует значения под типы полей: числа из строк, даты ISO, Select — ключ варианта
    /// (или Guid) → Guid, множественные — массив (JSON) или CSV, одиночное значение множественного
    /// поля заворачивается в массив. Query-поля и null-значения пропускаются.
    /// </summary>
    public static bool TryNormalize(IReadOnlyDictionary<string, JsonNode?> meta,
                                    IReadOnlyCollection<MetaFieldDto> fields,
                                    out Dictionary<string, JsonNode> result,
                                    out string? error)
    {
        result = [];
        error = null;

        foreach (var (key, node) in meta)
        {
            var field = fields.FirstOrDefault(f => f.Key == key);
            if (field is null)
            {
                var valid = string.Join(", ", fields.Where(f => f.Type != MetaFieldType.Query).Select(f => f.Key));
                error = $"поля '{key}' нет в типе поста; допустимые поля: {valid}";
                return false;
            }

            if (field.Type == MetaFieldType.Query || node is null) continue;

            if (field.Type == MetaFieldType.SelectMany)
            {
                if (!TrySelectMany(node, field, out var many, out error))
                {
                    error = FieldError(key, error);
                    return false;
                }
                result[key] = many;
                continue;
            }

            if (!TrySingleOrMulti(node, field, out var value, out error))
            {
                error = FieldError(key, error);
                return false;
            }
            result[key] = value;
        }

        return true;
    }

    //=====================================

    static string FieldError(string key, string? message) => $"поле '{key}': {message}";

    static bool TrySingleOrMulti(JsonNode node, MetaFieldDto field, out JsonNode result, out string? error)
    {
        result = null!;
        error = null;

        if (node is JsonArray array)
        {
            if (!field.IsMultiple)
            {
                error = "одиночное поле, массив значений не допускается";
                return false;
            }

            var items = new JsonArray();
            for (var i = 0; i < array.Count; i++)
            {
                if (array[i] is not { } element) continue;
                if (!TrySingle(element, field, out var item, out error))
                {
                    error = $"элемент {i}: {error}";
                    return false;
                }
                items.Add(item);
            }

            result = items;
            return true;
        }

        if (field.IsMultiple && IsString(node, out var csv))
        {
            var items = new JsonArray();
            foreach (var part in Split(csv))
            {
                if (!TrySingle(JsonValue.Create(part)!, field, out var item, out error)) return false;
                items.Add(item);
            }

            result = items;
            return true;
        }

        if (!TrySingle(node, field, out var single, out error)) return false;

        result = field.IsMultiple ? new JsonArray(single) : single;
        return true;
    }

    static bool TrySingle(JsonNode node, MetaFieldDto field, out JsonNode result, out string? error)
    {
        result = null!;
        error = null;

        switch (field.Type)
        {
            case MetaFieldType.String:
            case MetaFieldType.Text:
                result = WireString(NodeToText(node));
                return true;

            case MetaFieldType.Bool:
                if (node is JsonValue jsonBool && jsonBool.TryGetValue<bool>(out var native))
                {
                    result = Wire(native ? "true" : "false");
                    return true;
                }
                if (IsString(node, out var boolText) && bool.TryParse(boolText.Trim(), out var parsed))
                {
                    result = Wire(parsed ? "true" : "false");
                    return true;
                }
                if (node is JsonValue numBool && numBool.TryGetValue<double>(out var bit) && bit is 0 or 1)
                {
                    result = Wire(bit == 1 ? "true" : "false");
                    return true;
                }
                error = "ожидается true или false";
                return false;

            case MetaFieldType.Int:
            case MetaFieldType.Long:
                if (!TryLong(node, out var number, out error)) return false;
                if (field.Type == MetaFieldType.Int && number is < int.MinValue or > int.MaxValue)
                {
                    error = "число вне диапазона Int32";
                    return false;
                }
                result = Wire(number.ToString(CultureInfo.InvariantCulture));
                return true;

            case MetaFieldType.Float:
                if (!TryDouble(node, out var floating, out error)) return false;
                result = Wire(floating.ToString("R", CultureInfo.InvariantCulture));
                return true;

            case MetaFieldType.Decimal:
                if (!TryDecimal(node, out var money, out error)) return false;
                result = Wire(money.ToString(CultureInfo.InvariantCulture));
                return true;

            case MetaFieldType.DateTime:
                if (!IsString(node, out var dateText)
                    || !DateTimeOffset.TryParse(dateText.Trim(), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces, out _))
                {
                    error = "ожидается дата ISO-8601 строкой";
                    return false;
                }
                result = WireString(dateText.Trim());
                return true;

            case MetaFieldType.Relation:
            case MetaFieldType.File:
            case MetaFieldType.Image:
                if (!IsString(node, out var refText) || !Guid.TryParse(refText.Trim(), out var reference))
                {
                    error = "ожидается Guid — идентификатор объекта (для файла/картинки — из ListMedia или AddMedia)";
                    return false;
                }
                result = WireString(reference.ToString("D"));
                return true;

            case MetaFieldType.Select:
                return TrySelectVariant(node, field, out result, out error);

            default:
                error = $"неподдерживаемый тип поля '{field.Type}'";
                return false;
        }
    }

    static bool TrySelectVariant(JsonNode node, MetaFieldDto field, out JsonNode result, out string? error)
    {
        result = null!;

        if (!IsString(node, out var text))
        {
            error = "ожидается ключ варианта строкой";
            return false;
        }

        if (!TryResolveVariantId(text, field, out var id, out error)) return false;

        result = WireString(id.ToString("D"));
        return true;
    }

    static bool TryResolveVariantId(string text, MetaFieldDto field, out Guid id, out string? error)
    {
        id = Guid.Empty;
        error = null;

        var trimmed = text.Trim();
        var variants = field.Variants ?? [];

        var byKey = variants.FirstOrDefault(v => v.Key == trimmed);
        if (byKey is not null)
        {
            id = byKey.Id;
            return true;
        }

        if (Guid.TryParse(trimmed, out var parsed) && variants.Any(v => v.Id == parsed))
        {
            id = parsed;
            return true;
        }

        var available = string.Join(", ", variants.Select(v => v.Key));
        error = available.Length > 0
            ? $"вариант '{trimmed}' не найден; доступные ключи: {available}"
            : $"вариант '{trimmed}' не найден";
        return false;
    }

    static bool TrySelectMany(JsonNode node, MetaFieldDto field, out JsonNode result, out string? error)
    {
        result = null!;
        error = null;

        var texts = new List<string>();
        if (node is JsonArray array)
        {
            for (var i = 0; i < array.Count; i++)
            {
                if (array[i] is not { } element) continue;
                if (!IsString(element, out var itemText))
                {
                    error = $"элемент {i}: ожидается ключ варианта строкой";
                    return false;
                }
                texts.Add(itemText);
            }
        }
        else if (IsString(node, out var csv))
        {
            texts.AddRange(Split(csv));
        }
        else
        {
            error = "ожидается массив ключей вариантов или строка со списком через запятую";
            return false;
        }

        var ids = new List<Guid>();
        foreach (var text in texts)
        {
            if (!TryResolveVariantId(text, field, out var id, out error)) return false;
            ids.Add(id);
        }

        // CLR-узел: единственный вариант, который MetaValueFromJson читает как Guid[] (B5)
        result = JsonValue.Create(ids.ToArray())!;
        return true;
    }

    //=====================================

    static bool TryLong(JsonNode node, out long value, out string? error)
    {
        value = 0;
        error = null;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<long>(out var fromLong))
            {
                value = fromLong;
                return true;
            }
            if (json.TryGetValue<double>(out var fromDouble))
            {
                if (fromDouble % 1 != 0)
                {
                    error = "ожидается целое число";
                    return false;
                }
                value = (long)fromDouble;
                return true;
            }
            if (json.TryGetValue<string>(out var text)
                && long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        error = "ожидается целое число";
        return false;
    }

    static bool TryDouble(JsonNode node, out double value, out string? error)
    {
        value = 0;
        error = null;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<double>(out var fromDouble))
            {
                value = fromDouble;
                return true;
            }
            if (json.TryGetValue<string>(out var text)
                && double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        error = "ожидается число";
        return false;
    }

    static bool TryDecimal(JsonNode node, out decimal value, out string? error)
    {
        value = 0;
        error = null;

        if (node is JsonValue json)
        {
            if (json.TryGetValue<decimal>(out var fromDecimal))
            {
                value = fromDecimal;
                return true;
            }
            if (json.TryGetValue<double>(out var fromDouble))
            {
                value = (decimal)fromDouble;
                return true;
            }
            if (json.TryGetValue<string>(out var text)
                && decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        error = "ожидается число";
        return false;
    }

    static bool IsString(JsonNode node, out string text)
    {
        text = "";
        return node is JsonValue value && value.TryGetValue<string>(out text!);
    }

    static string NodeToText(JsonNode node)
        => IsString(node, out var text) ? text : node.ToJsonString();

    static IEnumerable<string> Split(string csv)
        => csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Узел из wire-JSON: GetValue&lt;T&gt; конвертирует его так же, как вход публичного API</summary>
    static JsonNode Wire(string jsonText) => JsonNode.Parse(jsonText)!;

    static JsonNode WireString(string text) => JsonNode.Parse(JsonSerializer.Serialize(text))!;
}
