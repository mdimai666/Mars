using System.Collections;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Mars.Forms.Contracts;

/// <summary>
/// Встроенные правила формы — чистые функции без доступа к данным владельца. Один код на клиент
/// и сервер: фронт проверяет значение сразу при вводе, серверный валидатор — перед записью.
/// Правила, которым нужны данные (например <see cref="FormRuleCatalog.Unique"/>),
/// остаются в реестре провайдера.
/// </summary>
public static class FormRuleEvaluator
{
    /// <summary>Сообщения правила для значения; пусто — значение соответствует правилу</summary>
    public static IEnumerable<string> Evaluate(FormRuleDefinition rule, object? value)
    {
        switch (rule.Type)
        {
            case FormRuleCatalog.Required:
                if (IsEmpty(value)) yield return Message(rule, "поле обязательно");
                break;

            case FormRuleCatalog.Regex:
                var pattern = ReadString(rule, "pattern");
                if (!string.IsNullOrEmpty(pattern)
                    && value is string text && text.Length > 0
                    && !Regex.IsMatch(text, pattern))
                {
                    yield return Message(rule, $"значение не соответствует шаблону '{pattern}'");
                }
                break;

            case FormRuleCatalog.Length:
                if (value is not string sized) break;
                if (ReadInt(rule, "min") is int min && sized.Length < min)
                    yield return Message(rule, $"минимальная длина {min}");
                if (ReadInt(rule, "max") is int max && sized.Length > max)
                    yield return Message(rule, $"максимальная длина {max}");
                break;

            case FormRuleCatalog.Min:
                if (Compare(rule, value, isMin: true) is { } minError) yield return minError;
                break;

            case FormRuleCatalog.Max:
                if (Compare(rule, value, isMin: false) is { } maxError) yield return maxError;
                break;
        }
    }

    /// <summary>Значение, приводимое к decimal (числа транспорта и строки инвариантной культуры)</summary>
    public static bool TryReadDecimal(object? value, out decimal result)
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

    static bool IsEmpty(object? value) => value switch
    {
        null => true,
        string text => text.Length == 0,
        IEnumerable items => !items.GetEnumerator().MoveNext(),
        _ => false,
    };

    static string? Compare(FormRuleDefinition rule, object? value, bool isMin)
    {
        if (!TryReadDecimal(value, out var actual)) return null;
        if (ReadDecimal(rule, "value") is not { } limit) return null;

        var violated = isMin ? actual < limit : actual > limit;
        if (!violated) return null;

        return Message(rule, isMin
            ? $"значение должно быть не меньше {limit.ToString(CultureInfo.InvariantCulture)}"
            : $"значение должно быть не больше {limit.ToString(CultureInfo.InvariantCulture)}");
    }

    static string Message(FormRuleDefinition rule, string fallback)
    {
        var message = ReadString(rule, "message");
        return string.IsNullOrEmpty(message) ? fallback : message;
    }

    static string? ReadString(FormRuleDefinition rule, string name)
        => rule.Params?[name] is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;

    static int? ReadInt(FormRuleDefinition rule, string name)
    {
        if (rule.Params?[name] is not JsonValue value) return null;
        if (value.TryGetValue<int>(out var result)) return result;
        if (value.TryGetValue<string>(out var text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        // параметр может прийти числом другой ширины — дочитываем из json-представления
        return int.TryParse(value.ToJsonString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var raw)
            ? raw
            : null;
    }

    static decimal? ReadDecimal(FormRuleDefinition rule, string name)
    {
        if (rule.Params?[name] is not JsonValue value) return null;
        if (value.TryGetValue<decimal>(out var money)) return money;
        if (value.TryGetValue<string>(out var text)
            && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return decimal.TryParse(value.ToJsonString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var raw)
            ? raw
            : null;
    }
}
