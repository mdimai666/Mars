using FluentValidation;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

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

        RuleFor(x => x).SetValidator(new MetaFieldsDuplicateQueryValidator(metaModelTypesLocator));
    }
}
