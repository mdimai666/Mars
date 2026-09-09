using System.Collections;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Validation;

/// <summary>
/// Встроенные правила общего слоя — чистые, без доступа к данным владельца.
/// Правила, которым нужны данные (<see cref="FormRuleCatalog.Unique"/>), регистрирует
/// провайдер в своём скоупе.
/// </summary>
public static class BuiltInFormRules
{
    static readonly Task<IEnumerable<string>> EmptyTask = Task.FromResult(Enumerable.Empty<string>());

    static ValueTask<IEnumerable<string>> Empty => new(EmptyTask);

    public static void RegisterAll(IFormRuleRegistry registry)
    {
        registry.Register(FormRuleCatalog.Required, Required);
        registry.Register(FormRuleCatalog.Regex, RegexRule);
        registry.Register(FormRuleCatalog.Length, Length);
        registry.Register(FormRuleCatalog.Min, Min);
        registry.Register(FormRuleCatalog.Max, Max);
    }

    static ValueTask<IEnumerable<string>> Required(object? value, JsonObject? parameters,
                                                   FormFieldValidationContext context, CancellationToken cancellationToken)
    {
        var empty = value switch
        {
            null => true,
            string text => text.Length == 0,
            IEnumerable items => !items.GetEnumerator().MoveNext(),
            _ => false,
        };

        return empty
            ? ValueTask.FromResult<IEnumerable<string>>([Message(parameters, "поле обязательно")])
            : Empty;
    }

    static ValueTask<IEnumerable<string>> RegexRule(object? value, JsonObject? parameters,
                                                    FormFieldValidationContext context, CancellationToken cancellationToken)
    {
        if (value is not string text || text.Length == 0) return Empty;

        var pattern = ReadString(parameters, "pattern");
        if (string.IsNullOrEmpty(pattern)) return Empty;

        return Regex.IsMatch(text, pattern)
            ? Empty
            : ValueTask.FromResult<IEnumerable<string>>(
                [Message(parameters, $"значение не соответствует шаблону '{pattern}'")]);
    }

    static ValueTask<IEnumerable<string>> Length(object? value, JsonObject? parameters,
                                                 FormFieldValidationContext context, CancellationToken cancellationToken)
    {
        if (value is not string text) return Empty;

        var min = ReadInt(parameters, "min");
        var max = ReadInt(parameters, "max");

        var errors = new List<string>();
        if (min is int minValue && text.Length < minValue)
            errors.Add(Message(parameters, $"минимальная длина {minValue}"));
        if (max is int maxValue && text.Length > maxValue)
            errors.Add(Message(parameters, $"максимальная длина {maxValue}"));

        return errors.Count == 0 ? Empty : ValueTask.FromResult<IEnumerable<string>>(errors);
    }

    static ValueTask<IEnumerable<string>> Min(object? value, JsonObject? parameters,
                                              FormFieldValidationContext context, CancellationToken cancellationToken)
        => Compare(value, parameters, isMin: true);

    static ValueTask<IEnumerable<string>> Max(object? value, JsonObject? parameters,
                                              FormFieldValidationContext context, CancellationToken cancellationToken)
        => Compare(value, parameters, isMin: false);

    static ValueTask<IEnumerable<string>> Compare(object? value, JsonObject? parameters, bool isMin)
    {
        if (!TryReadDecimal(value, out var actual)) return Empty;

        var limit = ReadDecimal(parameters, "value");
        if (limit is null) return Empty;

        var violated = isMin ? actual < limit : actual > limit;
        return violated
            ? ValueTask.FromResult<IEnumerable<string>>(
                [Message(parameters, isMin
                    ? $"значение должно быть не меньше {limit.Value.ToString(CultureInfo.InvariantCulture)}"
                    : $"значение должно быть не больше {limit.Value.ToString(CultureInfo.InvariantCulture)}")])
            : Empty;
    }

    //=====================================

    static string Message(JsonObject? parameters, string fallback)
    {
        var message = ReadString(parameters, "message");
        return string.IsNullOrEmpty(message) ? fallback : message;
    }

    internal static bool TryReadDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case decimal money:
                result = money;
                return true;
            case double floating:
                result = (decimal)floating;
                return true;
            case long number:
                result = number;
                return true;
            case int number:
                result = number;
                return true;
            case string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    static string? ReadString(JsonObject? obj, string name)
        => obj?[name] is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;

    static int? ReadInt(JsonObject? obj, string name)
    {
        if (obj?[name] is not JsonValue value) return null;
        if (value.TryGetValue<int>(out var result)) return result;
        if (value.TryGetValue<string>(out var text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        // параметр может прийти числом другой ширины — дочитываем из json-представления
        return int.TryParse(value.ToJsonString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var raw)
            ? raw
            : null;
    }

    static decimal? ReadDecimal(JsonObject? obj, string name)
    {
        if (obj?[name] is not JsonValue value) return null;
        if (value.TryGetValue<decimal>(out var money)) return money;
        if (value.TryGetValue<string>(out var text)
            && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return decimal.TryParse(value.ToJsonString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var raw)
            ? raw
            : null;
    }
}
