using FluentValidation;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.PostTypes;
using Mars.Core.Exceptions;

namespace Mars.Cms.Abstractions.Dto.Posts;

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
