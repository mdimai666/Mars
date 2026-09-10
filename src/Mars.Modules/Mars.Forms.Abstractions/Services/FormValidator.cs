using System.Text.Json.Nodes;
using Mars.Forms.Abstractions.Validation;
using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Services;

internal class FormValidator(IFormRuleRegistry rules) : IFormValidator
{
    public async Task<IReadOnlyCollection<FormError>> ValidateAsync(FormDefinition definition, FormValues values,
                                                                    FormValidationContext? context = null,
                                                                    CancellationToken cancellationToken = default)
    {
        var errors = new List<FormError>();

        foreach (var item in definition.Fields())
        {
            var field = item.Field;
            // вычислимые поля не отправляются, read-only не редактируются — проверять нечего
            if (field is null || field.Type == FormFieldType.Computed || field.ReadOnly) continue;

            values.Values.TryGetValue(field.Key, out var node);

            var fieldRules = field.Rules;
            var required = field.Required || fieldRules.Any(r => r.Type == FormRuleCatalog.Required);

            if (FormValueCodec.IsEmpty(node))
            {
                if (required)
                    errors.Add(new FormError { Key = field.Key, Message = "поле обязательно" });
                continue;
            }

            if (!FormValueCodec.IsShapeValid(field, node, out var shapeError))
            {
                errors.Add(new FormError { Key = field.Key, Message = shapeError ?? "неверный формат значения" });
                continue;
            }

            var fieldContext = new FormFieldValidationContext
            {
                OwnerModel = definition.OwnerModel,
                OwnerId = context?.OwnerId,
                Field = field,
                Services = context?.Services,
            };

            foreach (var (index, value) in ReadValues(field, node))
            {
                if (CheckLimits(field, value, out var limitError))
                    errors.Add(new FormError { Key = field.Key, Index = index, Message = limitError! });

                foreach (var rule in fieldRules)
                {
                    foreach (var message in await rules.ValidateAsync(rule, value, fieldContext, cancellationToken))
                        errors.Add(new FormError { Key = field.Key, Index = index, Message = message });
                }
            }
        }

        return errors;
    }

    static IReadOnlyList<(int Index, object? Value)> ReadValues(FormFieldDescriptor field, JsonNode? node)
    {
        if (field.Multiple)
        {
            if (!FormValueCodec.TryToClrList(node, field.Type, out var list, out _)) return [];
            return list.Select((value, index) => (index, value)).ToList();
        }

        FormValueCodec.TryToClr(node, field.Type, out var single, out _);
        return [(0, single)];
    }

    static bool CheckLimits(FormFieldDescriptor field, object? value, out string? error)
    {
        error = null;
        if (field.Min is null && field.Max is null) return false;
        if (!BuiltInFormRules.TryReadDecimal(value, out var actual)) return false;

        if (field.Min is decimal min && actual < min)
        {
            error = $"значение должно быть не меньше {min}";
            return true;
        }
        if (field.Max is decimal max && actual > max)
        {
            error = $"значение должно быть не больше {max}";
            return true;
        }

        return false;
    }
}
