using FluentValidation;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.PostTypes;
using Mars.Cms.Abstractions.Validators;

namespace Mars.Cms.Abstractions.Dto.Posts;

public class CreatePostQueryValidator : AbstractValidator<CreatePostQuery>
{
    public CreatePostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator,
                                    IPostCategoryRepository postCategoryRepository,
                                    IMetaValuesValidator metaValuesValidator,
                                    IPostRepository postRepository)
    {
        RuleFor(x => x).SetValidator(new GeneralPostQueryValidator(metaModelTypesLocator, postCategoryRepository));

        RuleFor(x => x.MetaValues).ValidateMetaValues(metaValuesValidator, MetaValueOwnerCatalog.Post, x => x.Id);

        RuleFor(x => x)
            .CustomAsync(async (x, context, cancellationToken) =>
            {
                var postType = metaModelTypesLocator.GetPostTypeByName(x.Type);
                if (postType is null || !postType.EnabledFeatures.Contains(PostTypeConstants.Features.Single)) return;

                var postCount = await postRepository.CountByTypeAsync(postType.Id, cancellationToken);
                if (postCount > 0)
                {
                    context.AddFailure(nameof(x.Type),
                        $"Тип '{x.Type}' допускает единственную запись (фича «Единственная запись») — она уже существует");
                }
            });
    }
}
