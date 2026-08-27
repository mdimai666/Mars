using FluentValidation;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.Posts;

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
