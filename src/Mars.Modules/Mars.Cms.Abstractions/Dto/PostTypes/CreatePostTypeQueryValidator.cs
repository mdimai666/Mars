using FluentValidation;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

public class CreatePostTypeQueryValidator : AbstractValidator<CreatePostTypeQuery>
{
    public CreatePostTypeQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x).SetValidator(new GeneralPostTypeQueryValidator());

        RuleFor(x => x.TypeName)
            .Must(name => metaModelTypesLocator.GetPostTypeByName(name) == null)
            .WithMessage(x => $"Post type '{x.TypeName}' already exist");

        RuleFor(x => x)
            .Custom((x, context) =>
            {
                if (!x.EnabledFeatures.Contains(PostTypeConstants.Features.PostImage)) return;

                if (string.IsNullOrEmpty(x.ImageFieldKey))
                {
                    context.AddFailure(nameof(x.ImageFieldKey), "Выберите поле картинки для фичи «Картинка поста»");
                    return;
                }

                if (!x.MetaFields.Any(f => f.Key == x.ImageFieldKey && f.Type == MetaFieldType.Image))
                {
                    context.AddFailure(nameof(x.ImageFieldKey), $"Поле картинки «{x.ImageFieldKey}» не найдено среди полей типа или не является изображением");
                }
            });

        RuleFor(x => x)
            .Custom((x, context) =>
            {
                if (!x.EnabledFeatures.Contains(PostTypeConstants.Features.Content)) return;

                if (!x.MetaFields.Any(f => f.Key == FeatureFieldsCatalog.ContentFieldKey
                                           && (f.Type == MetaFieldType.String || f.Type == MetaFieldType.Text)))
                {
                    context.AddFailure(nameof(x.MetaFields),
                        $"Для фичи «Контент» требуется поле с ключом «{FeatureFieldsCatalog.ContentFieldKey}» типа Текст или Строка");
                }
            });

        RuleFor(x => x).SetValidator(new MetaFieldsDuplicateQueryValidator(metaModelTypesLocator));
    }
}
