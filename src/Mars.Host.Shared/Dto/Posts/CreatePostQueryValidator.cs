using FluentValidation;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.Posts;

public class CreatePostQueryValidator : AbstractValidator<CreatePostQuery>
{
    public CreatePostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator, IPostCategoryRepository postCategoryRepository)
    {
        RuleFor(x => x).SetValidator(new GeneralPostQueryValidator(metaModelTypesLocator, postCategoryRepository));
    }
}
