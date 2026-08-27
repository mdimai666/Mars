using FluentValidation;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.Posts;

public class DeletePostQueryValidator : AbstractValidator<Guid>
{
    public DeletePostQueryValidator(IPostRepository postRepository, IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x)
            .CustomAsync(async (id, context, ct) =>
            {
                var post = await postRepository.Get(id, ct);

                if (post == null)
                {
                    throw new NotFoundException($"post '{id}' not exist");
                }

                var postType = metaModelTypesLocator.GetPostTypeByName(post.Type);
                if (postType is not null && postType.EnabledFeatures.Contains(PostTypeConstants.Features.Single))
                {
                    context.AddFailure("Id", $"Запись типа '{post.Type}' единственная (фича «Единственная запись») — удаление запрещено");
                }
            });
    }
}
