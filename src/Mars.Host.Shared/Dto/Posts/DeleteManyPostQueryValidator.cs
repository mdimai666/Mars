using FluentValidation;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.Posts;

public class DeleteManyPostQueryValidator : AbstractValidator<DeleteManyPostQuery>
{
    public DeleteManyPostQueryValidator(IPostRepository postRepository)
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("post type ids for delete must not be empty");

        RuleForEach(x => x.Ids)
            .SetValidator(new DeletePostQueryValidator(postRepository));
    }
}
