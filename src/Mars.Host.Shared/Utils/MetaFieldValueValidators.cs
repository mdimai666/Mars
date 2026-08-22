using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Utils;

/// <summary>
/// Реестр валидаторов значений мета-полей. Встроенные — из <see cref="MetaFieldValidatorCatalog"/>;
/// расширение — <see cref="Register"/> (плагины/модули на старте).
/// </summary>
public static class MetaFieldValueValidators
{
    public delegate IEnumerable<string> Validator(object? value, JsonObject? parameters);

    static readonly Dictionary<string, Validator> _registry = new()
    {
        [MetaFieldValidatorCatalog.Regex] = ValidateRegex,
        [MetaFieldValidatorCatalog.Length] = ValidateLength,
    };

    /// <summary>Регистрирует/заменяет валидатор по дискриминатору</summary>
    public static void Register(string type, Validator handler)
        => _registry[type] = handler;

    public static bool IsKnown(string type)
        => _registry.ContainsKey(type);

    public static IEnumerable<string> Validate(MetaFieldValidatorDefinition rule, object? value)
        => _registry.TryGetValue(rule.Type, out var handler) ? handler(value, rule.Params) : [];

    static IEnumerable<string> ValidateRegex(object? value, JsonObject? parameters)
    {
        if (value is not string text || text.Length == 0) yield break;

        var pattern = ReadString(parameters, "pattern");
        if (string.IsNullOrEmpty(pattern)) yield break;

        if (!Regex.IsMatch(text, pattern))
        {
            yield return ReadString(parameters, "message") is { Length: > 0 } custom
                ? custom
                : $"значение не соответствует шаблону '{pattern}'";
        }
    }

    static IEnumerable<string> ValidateLength(object? value, JsonObject? parameters)
    {
        if (value is not string text) yield break;

        var min = ReadInt(parameters, "min");
        var max = ReadInt(parameters, "max");

        if (min is int minValue && text.Length < minValue)
            yield return $"минимальная длина {minValue}";
        if (max is int maxValue && text.Length > maxValue)
            yield return $"максимальная длина {maxValue}";
    }

    static string? ReadString(JsonObject? obj, string name)
        => obj?[name] is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;

    static int? ReadInt(JsonObject? obj, string name)
        => obj?[name] is JsonValue value && value.TryGetValue<int>(out var result) ? result : null;
}
