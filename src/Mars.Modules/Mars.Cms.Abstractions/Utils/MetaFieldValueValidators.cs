using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Contracts.MetaFields;

namespace Mars.Cms.Abstractions.Utils;

/// <summary>
/// Реестр валидаторов значений мета-полей. Встроенные — из <see cref="MetaFieldValidatorCatalog"/>;
/// расширение — <see cref="Register"/> (плагины/модули на старте).
/// </summary>
public static class MetaFieldValueValidators
{
    public delegate ValueTask<IEnumerable<string>> Validator(object? value, JsonObject? parameters,
                                                             MetaValueValidationContext context, CancellationToken cancellationToken);

    static readonly Task<IEnumerable<string>> _emptyTask = Task.FromResult(Enumerable.Empty<string>());

    static ValueTask<IEnumerable<string>> Empty => new(_emptyTask);

    static readonly Dictionary<string, Validator> _registry = new()
    {
        [MetaFieldValidatorCatalog.Regex] = ValidateRegex,
        [MetaFieldValidatorCatalog.Length] = ValidateLength,
        [MetaFieldValidatorCatalog.Unique] = ValidateUnique,
    };

    /// <summary>Регистрирует/заменяет валидатор по дискриминатору</summary>
    public static void Register(string type, Validator handler)
        => _registry[type] = handler;

    public static bool IsKnown(string type)
        => _registry.ContainsKey(type);

    public static ValueTask<IEnumerable<string>> ValidateAsync(MetaFieldValidatorDefinition rule, object? value,
                                                               MetaValueValidationContext context, CancellationToken cancellationToken)
        => _registry.TryGetValue(rule.Type, out var handler)
            ? handler(value, rule.Params, context, cancellationToken)
            : Empty;

    static ValueTask<IEnumerable<string>> ValidateRegex(object? value, JsonObject? parameters,
                                                        MetaValueValidationContext context, CancellationToken cancellationToken)
    {
        if (value is not string text || text.Length == 0) return Empty;

        var pattern = ReadString(parameters, "pattern");
        if (string.IsNullOrEmpty(pattern)) return Empty;

        if (!Regex.IsMatch(text, pattern))
        {
            var message = ReadString(parameters, "message") is { Length: > 0 } custom
                ? custom
                : $"значение не соответствует шаблону '{pattern}'";
            return ValueTask.FromResult<IEnumerable<string>>([message]);
        }

        return Empty;
    }

    static ValueTask<IEnumerable<string>> ValidateLength(object? value, JsonObject? parameters,
                                                         MetaValueValidationContext context, CancellationToken cancellationToken)
    {
        if (value is not string text) return Empty;

        var min = ReadInt(parameters, "min");
        var max = ReadInt(parameters, "max");

        var errors = new List<string>();
        if (min is int minValue && text.Length < minValue)
            errors.Add($"минимальная длина {minValue}");
        if (max is int maxValue && text.Length > maxValue)
            errors.Add($"максимальная длина {maxValue}");

        return errors.Count == 0 ? Empty : ValueTask.FromResult<IEnumerable<string>>(errors);
    }

    static async ValueTask<IEnumerable<string>> ValidateUnique(object? value, JsonObject? parameters,
                                                               MetaValueValidationContext context, CancellationToken cancellationToken)
    {
        if (value is null || (value is string text && text.Length == 0)) return [];
        if (context.Field is null || context.UniquenessProvider is null) return [];

        var occupied = await context.UniquenessProvider.IsOccupiedAsync(context.Field, value, context.OwnerId, cancellationToken);
        return occupied ? ["значение уже занято"] : [];
    }

    static string? ReadString(JsonObject? obj, string name)
        => obj?[name] is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;

    static int? ReadInt(JsonObject? obj, string name)
        => obj?[name] is JsonValue value && value.TryGetValue<int>(out var result) ? result : null;
}
