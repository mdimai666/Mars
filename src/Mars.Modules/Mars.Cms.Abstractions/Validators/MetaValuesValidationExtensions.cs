using FluentValidation;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Validators;

namespace Mars.Cms.Abstractions.Validators;

/// <summary>
/// Подключение проверки мета-значений (<see cref="IMetaValuesValidator"/>) к валидаторам запросов
/// </summary>
public static class MetaValuesValidationExtensions
{
    public static void ValidateMetaValues<T>(
        this IRuleBuilder<T, IReadOnlyCollection<ModifyMetaValueDetailQuery>?> ruleBuilder,
        IMetaValuesValidator validator,
        string ownerModel,
        Func<T, Guid?>? ownerIdSelector = null)
        => ruleBuilder.CustomAsync(async (values, context, cancellationToken) =>
        {
            if (values is null) return;

            var ownerContext = new MetaValueValidationContext
            {
                ModelName = ownerModel,
                OwnerId = ownerIdSelector is null ? null : ownerIdSelector(context.InstanceToValidate),
            };

            foreach (var error in await validator.ValidateAsync(values, ownerContext, cancellationToken))
                context.AddFailure("MetaValues", $"поле '{error.FieldKey}': {error.Message}");
        });
}
