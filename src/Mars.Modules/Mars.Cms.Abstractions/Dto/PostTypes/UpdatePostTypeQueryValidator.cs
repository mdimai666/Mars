using FluentValidation;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

public class UpdatePostTypeQueryValidator : AbstractValidator<UpdatePostTypeQuery>
{
    public UpdatePostTypeQueryValidator(IMetaModelTypesLocator metaModelTypesLocator, IPostRepository postRepository)
    {
        RuleFor(x => x).SetValidator(new GeneralPostTypeQueryValidator());

        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var postType = metaModelTypesLocator.GetPostTypeByName(x.TypeName);

                if (postType is not null && postType.Id != x.Id)
                {
                    context.AddFailure(nameof(x.TypeName), $"Post type '{x.TypeName}' already exist");
                    return;
                }

            });

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

        RuleFor(x => x)
            .CustomAsync(async (x, context, cancellationToken) =>
            {
                if (!x.EnabledFeatures.Contains(PostTypeConstants.Features.Single)) return;

                var postCount = await postRepository.CountByTypeAsync(x.Id, cancellationToken);
                if (postCount > 1)
                {
                    context.AddFailure(nameof(x.EnabledFeatures),
                        $"Нельзя включить «Единственная запись»: у типа уже {postCount} записей");
                }
            });

        RuleFor(x => x).SetValidator(new MetaFieldsDuplicateQueryValidator(metaModelTypesLocator));
    }
}
