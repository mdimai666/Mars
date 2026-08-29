using FluentValidation;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;

namespace Mars.Cms.Abstractions.Dto.Posts;

public class DeleteManyPostQueryValidator : AbstractValidator<DeleteManyPostQuery>
{
    public DeleteManyPostQueryValidator(IPostRepository postRepository, IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("post type ids for delete must not be empty");

        RuleForEach(x => x.Ids)
            .SetValidator(new DeletePostQueryValidator(postRepository, metaModelTypesLocator));
    }
}
