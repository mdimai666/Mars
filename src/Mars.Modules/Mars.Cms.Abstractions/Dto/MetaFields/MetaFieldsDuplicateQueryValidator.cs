using FluentValidation;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.MetaFields;
using Mars.Cms.Abstractions.Utils;

namespace Mars.Cms.Abstractions.Dto.MetaFields;

public class MetaFieldsDuplicateQueryValidator : AbstractValidator<IGeneralMetaFieldsSupportDto>
{
    public MetaFieldsDuplicateQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleForEach(x => x.MetaFields)
            .ChildRules(metaField =>
            {
                metaField.RuleFor(x => x.Key)
                    .NotEmpty()
                    .WithMessage("Key не может быть пустым");

                metaField.RuleFor(x => x.Key)
                    .Matches(MetaFieldKeyNormalizer.FormatPattern)
                    .WithMessage("Key должен соответствовать формату [a-z_][a-z0-9_]*");

                metaField.RuleFor(x => x.Variants)
                    .Custom((variants, context) =>
                    {
                        if (variants is null) return;

                        var duplicates = variants
                            .Select(v => v.Key)
                            .Where(k => !string.IsNullOrEmpty(k))
                            .GroupBy(k => k)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key);

                        foreach (var key in duplicates)
                        {
                            context.AddFailure($"Variant с ключом '{key}' дублируется");
                        }
                    });

                metaField.RuleFor(x => x)
                    .Custom((field, context) => ValidateModelName(field!, metaModelTypesLocator, context));

                metaField.RuleFor(x => x)
                    .Custom((field, context) => ValidateKind(field!, context));
            });

        RuleFor(x => x.MetaFields)
            .Custom((metaFields, context) =>
            {
                var duplicates = metaFields
                    .GroupBy(m => m.Key)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.Skip(1));

                foreach (var duplicate in duplicates)
                {
                    var index = metaFields.ToArray().IndexOf(duplicate);

                    context.AddFailure($"MetaFields[{index}].Key", $"MetaField with key '{duplicate.Key}' дублируется");
                }
            });
    }

    /// <summary>
    /// Валидация <see cref="MetaFieldDto.ModelName"/> по реестру известных целей связей
    /// </summary>
    static void ValidateModelName(MetaFieldDto field, IMetaModelTypesLocator metaModelTypesLocator, ValidationContext<MetaFieldDto> context)
    {
        if (field.Type is MetaFieldType.File or MetaFieldType.Image)
        {
            if (!string.IsNullOrEmpty(field.ModelName))
                context.AddFailure(nameof(field.ModelName), $"ModelName должен быть пустым для типа поля '{field.Type}'");
            return;
        }

        if (field.Type != MetaFieldType.Relation) return;

        if (string.IsNullOrEmpty(field.ModelName))
        {
            context.AddFailure(nameof(field.ModelName), "ModelName должен быть указан для поля типа Relation");
            return;
        }

        var parts = field.ModelName.Split('.', 2);
        var root = parts[0];

        if (!metaModelTypesLocator.ListMetaRelationModelProviderKeys().Contains(root))
        {
            context.AddFailure(nameof(field.ModelName), $"Цель связи '{field.ModelName}' не найдена в реестре целей");
            return;
        }

        if (root == "Post" && parts.Length > 1 && !metaModelTypesLocator.ExistPostType(parts[1]))
        {
            context.AddFailure(nameof(field.ModelName), $"Тип поста '{parts[1]}' цели связи не существует");
        }
    }

    /// <summary>
    /// Вид «список объектов» (секция детей) доступен только множественному
    /// Relation-полю с целью-типом поста (<c>Post.&lt;тип&gt;</c>).
    /// </summary>
    static void ValidateKind(MetaFieldDto field, ValidationContext<MetaFieldDto> context)
    {
        var kind = field.Options.GetKind();
        if (kind != MetaFieldKindCatalog.List) return;

        if (field.Type != MetaFieldType.Relation)
        {
            context.AddFailure(nameof(field.Options), $"Вид '{kind}' доступен только полям типа Relation");
            return;
        }

        if (!field.IsMultiple)
        {
            context.AddFailure(nameof(field.Options), $"Вид '{kind}' доступен только полям с несколькими значениями");
            return;
        }

        if (field.ModelName?.StartsWith("Post.") != true)
        {
            context.AddFailure(nameof(field.Options), $"Вид '{kind}' доступен только связям с целью-типом поста (Post.<тип>)");
        }
    }
}
