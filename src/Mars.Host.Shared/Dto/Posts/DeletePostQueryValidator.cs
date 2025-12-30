using FluentValidation;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Repositories;

namespace Mars.Host.Shared.Dto.Posts;

public class DeletePostQueryValidator : AbstractValidator<Guid>
{
    public DeletePostQueryValidator(IPostRepository postRepository)
    {
        RuleFor(x => x)
            .CustomAsync(async (id, context, ct) =>
            {
                var post = await postRepository.Get(id, ct);

                if (post == null)
                {
                    throw new NotFoundException($"post '{id}' not exist");
                }
            });
    }
}
