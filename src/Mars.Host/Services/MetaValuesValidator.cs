using System.Globalization;
using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Services;

internal class MetaValuesValidator : IMetaValuesValidator
{
    public IReadOnlyCollection<MetaValueValidationError> Validate(IReadOnlyCollection<ModifyMetaValueDetailQuery> values)
    {
        var errors = new List<MetaValueValidationError>();
        foreach (var value in values)
        {
            foreach (var message in ValidateField(value.MetaField, value.GetValueSimple()))
                errors.Add(new MetaValueValidationError(value.MetaField.Key, message));
        }

        return errors;
    }

    public IReadOnlyCollection<MetaValueValidationError> ValidateJson(IReadOnlyCollection<MetaFieldDto> fields,
                                                                      IReadOnlyDictionary<string, JsonNode>? meta,
                                                                      bool requireAll)
    {
        var errors = new List<MetaValueValidationError>();
        foreach (var field in fields)
        {
            if (field.Type == MetaFieldType.Query) continue;

            JsonNode? node = null;
            var present = meta is not null && meta.TryGetValue(field.Key, out node) && node is not null;
            if (!present)
            {
                // поле с генератором будет заполнено при создании — отсутствие значения не ошибка
                if (requireAll && !field.IsNullable && MetaFieldGeneratorDefinition.FromOptions(field.Options) is null)
                    errors.Add(new MetaValueValidationError(field.Key, "значение обязательно"));
                continue;
            }

            foreach (var message in ValidateField(field, JsonToValue(field, node!)))
                errors.Add(new MetaValueValidationError(field.Key, message));
        }

        return errors;
    }

    /// <summary>Обязательность, диапазон Min/Max и правила из Options.validators</summary>
    static IEnumerable<string> ValidateField(MetaFieldDto field, object? value)
    {
        if (value is null)
        {
            if (!field.IsNullable) yield return "значение обязательно";
            yield break;
        }

        if (field.MinValue is not null || field.MaxValue is not null)
        {
            switch (value)
            {
                case string text:
                    if (field.MinValue is not null && text.Length < field.MinValue)
                        yield return $"минимальная длина {field.MinValue}";
                    if (field.MaxValue is not null && text.Length > field.MaxValue)
                        yield return $"максимальная длина {field.MaxValue}";
                    break;

                case int or long or double or decimal:
                    var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    if (field.MinValue is not null && number < field.MinValue)
                        yield return $"минимальное значение {field.MinValue}";
                    if (field.MaxValue is not null && number > field.MaxValue)
                        yield return $"максимальное значение {field.MaxValue}";
                    break;
            }
        }

        foreach (var rule in MetaFieldValidatorDefinition.FromOptions(field.Options))
        {
            foreach (var message in MetaFieldValueValidators.Validate(rule, value))
                yield return message;
        }
    }

    /// <summary>Извлечение типизированного значения из json для проверки; нескалярные узлы — маркер наличия</summary>
    static object? JsonToValue(MetaFieldDto field, JsonNode node)
    {
        if (node is JsonArray) return node;

        if (node is not JsonValue value) return node;

        // явные возвраты: тернарник с node неявно приводил бы значения к JsonNode
        switch (field.Type)
        {
            case MetaFieldType.String:
            case MetaFieldType.Text:
                if (value.TryGetValue<string>(out var text)) return text;
                return node;

            case MetaFieldType.Bool:
                if (value.TryGetValue<bool>(out var b)) return b;
                return node;

            case MetaFieldType.Int:
                if (value.TryGetValue<int>(out var i)) return i;
                return node;

            case MetaFieldType.Long:
                if (value.TryGetValue<long>(out var l)) return l;
                return node;

            case MetaFieldType.Float:
                if (value.TryGetValue<double>(out var d)) return d;
                return node;

            case MetaFieldType.Decimal:
                if (value.TryGetValue<decimal>(out var m)) return m;
                return node;

            case MetaFieldType.DateTime:
                if (value.TryGetValue<DateTime>(out var dt)) return dt;
                return node;

            default:
                return node;
        }
    }
}
