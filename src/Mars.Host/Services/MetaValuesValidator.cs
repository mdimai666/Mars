using System.Globalization;
using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Utils;
using Mars.Shared.Contracts.MetaFields;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

internal class MetaValuesValidator : IMetaValuesValidator
{
    private readonly IServiceProvider _serviceProvider;

    public MetaValuesValidator(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public async Task<IReadOnlyCollection<MetaValueValidationError>> ValidateAsync(IReadOnlyCollection<ModifyMetaValueDetailQuery> values,
                                                                                   MetaValueValidationContext context,
                                                                                   CancellationToken cancellationToken = default)
    {
        var errors = new List<MetaValueValidationError>();
        var domainContext = WithDomainProvider(context);
        foreach (var value in values)
        {
            var fieldContext = domainContext with { Field = value.MetaField };
            foreach (var message in await ValidateFieldAsync(value.MetaField, value.GetValueSimple(), fieldContext, cancellationToken))
                errors.Add(new MetaValueValidationError(value.MetaField.Key, message));
        }

        return errors;
    }

    public async Task<IReadOnlyCollection<MetaValueValidationError>> ValidateJsonAsync(IReadOnlyCollection<MetaFieldDto> fields,
                                                                                       IReadOnlyDictionary<string, JsonNode>? meta,
                                                                                       bool requireAll,
                                                                                       MetaValueValidationContext context,
                                                                                       string? contentFieldKey = null,
                                                                                       CancellationToken cancellationToken = default)
    {
        var errors = new List<MetaValueValidationError>();
        var domainContext = WithDomainProvider(context);
        foreach (var field in fields)
        {
            if (field.Type == MetaFieldType.Query) continue;
            if (contentFieldKey is not null && field.Key == contentFieldKey) continue; // значение — в posts.Content

            JsonNode? node = null;
            var present = meta is not null && meta.TryGetValue(field.Key, out node) && node is not null;
            if (!present)
            {
                // поле с генератором будет заполнено при создании — отсутствие значения не ошибка
                if (requireAll && !field.IsNullable && MetaFieldGeneratorDefinition.FromOptions(field.Options) is null)
                    errors.Add(new MetaValueValidationError(field.Key, "значение обязательно"));
                continue;
            }

            var fieldContext = domainContext with { Field = field };
            foreach (var message in await ValidateFieldAsync(field, JsonToValue(field, node!), fieldContext, cancellationToken))
                errors.Add(new MetaValueValidationError(field.Key, message));
        }

        return errors;
    }

    /// <summary>Провайдер домена владельца (по ключу модели) — для правил,
    /// обращающихся к данным; провайдер не зарегистрирован — правила пропускаются</summary>
    MetaValueValidationContext WithDomainProvider(MetaValueValidationContext context)
        => context with
        {
            UniquenessProvider = context.ModelName is null
                ? null
                : _serviceProvider.GetKeyedService<IMetaValueUniquenessProvider>(context.ModelName),
        };

    /// <summary>Обязательность, диапазон Min/Max и правила из Options.validators</summary>
    static async Task<List<string>> ValidateFieldAsync(MetaFieldDto field, object? value,
                                                       MetaValueValidationContext context, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (value is null)
        {
            if (!field.IsNullable) errors.Add("значение обязательно");
            return errors;
        }

        if (field.MinValue is not null || field.MaxValue is not null)
        {
            switch (value)
            {
                case string text:
                    if (field.MinValue is not null && text.Length < field.MinValue)
                        errors.Add($"минимальная длина {field.MinValue}");
                    if (field.MaxValue is not null && text.Length > field.MaxValue)
                        errors.Add($"максимальная длина {field.MaxValue}");
                    break;

                case int or long or double or decimal:
                    var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    if (field.MinValue is not null && number < field.MinValue)
                        errors.Add($"минимальное значение {field.MinValue}");
                    if (field.MaxValue is not null && number > field.MaxValue)
                        errors.Add($"максимальное значение {field.MaxValue}");
                    break;
            }
        }

        foreach (var rule in MetaFieldValidatorDefinition.FromOptions(field.Options))
            errors.AddRange(await MetaFieldValueValidators.ValidateAsync(rule, value, context, cancellationToken));

        return errors;
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
