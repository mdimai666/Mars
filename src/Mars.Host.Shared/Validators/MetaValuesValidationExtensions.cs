using FluentValidation;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Validators;

/// <summary>
/// Подключение проверки мета-значений (<see cref="IMetaValuesValidator"/>) к валидаторам запросов
/// </summary>
public static class MetaValuesValidationExtensions
{
    public static void ValidateMetaValues<T>(
        this IRuleBuilder<T, IReadOnlyCollection<ModifyMetaValueDetailQuery>?> ruleBuilder,
        IMetaValuesValidator validator)
        => ruleBuilder.Custom((values, context) => AddFailures(values, context, validator));

    static void AddFailures<T>(IReadOnlyCollection<ModifyMetaValueDetailQuery>? values,
                               ValidationContext<T> context,
                               IMetaValuesValidator validator)
    {
        if (values is null) return;

        foreach (var error in validator.Validate(values))
            context.AddFailure("MetaValues", $"поле '{error.FieldKey}': {error.Message}");
    }
}
