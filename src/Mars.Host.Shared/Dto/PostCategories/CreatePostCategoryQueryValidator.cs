using FluentValidation;
using Mars.Core.Features;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostCategories;

public class CreatePostCategoryQueryValidator : AbstractValidator<CreatePostCategoryQuery>
{
    public CreatePostCategoryQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator,
                                            IPostCategoryRepository postCategoryRepository,
                                            IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .Must(TextTool.IsValidSlugWithUpperCase)
            .WithMessage(v => $"'{v.Slug}' is Invalid slug");

        RuleFor(x => x.ParentId)
            .Must((x, id) => x.ParentId != x.Id)
            .When(x => x.ParentId != null)
            .WithMessage(v => $"ParentId cannot be inself");

        RuleFor(x => x.PostTypeId)
           .Must(metaModelTypesLocator.ExistPostType)
           .WithMessage(x => $"PostType Id '{x.PostTypeId}' not found");

        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var postCategoryType = postCategoryMetaLocator.GetTypeDetailById(x.PostCategoryTypeId);

                if (postCategoryType == null)
                {
                    context.AddFailure(nameof(x.PostCategoryTypeId), $"postCategoryType type '{x.PostCategoryTypeId}' not exist");
                    return;
                }

            });
    }
}
