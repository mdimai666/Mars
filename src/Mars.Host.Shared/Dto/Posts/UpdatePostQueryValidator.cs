using FluentValidation;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.Posts;

public class UpdatePostQueryValidator : AbstractValidator<UpdatePostQuery>
{
    public UpdatePostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator, IPostRepository postRepository, IPostCategoryRepository postCategoryRepository)
    {
        RuleFor(x => x).SetValidator(new GeneralPostQueryValidator(metaModelTypesLocator, postCategoryRepository));

        RuleFor(x => x.Id)
            .MustAsync(postRepository.ExistAsync)
            .WithMessage(x => $"Post Id '{x.Id}' not found");

    }
}
