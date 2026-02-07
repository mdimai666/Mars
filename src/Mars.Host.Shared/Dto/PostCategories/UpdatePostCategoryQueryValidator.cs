using FluentValidation;
using Mars.Core.Features;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostCategories;

public class UpdatePostCategoryQueryValidator : AbstractValidator<UpdatePostCategoryQuery>
{
    public UpdatePostCategoryQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator,
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
            .WithMessage(v => $"ParentId cannot be inself")
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .CustomAsync(async (x, context, cancellationToken) =>
                    {
                        if (x.ParentId is not null)
                        {
                            var parent = await postCategoryRepository.GetDetail(x.ParentId.Value, cancellationToken);
                            if (parent == null)
                            {
                                context.AddFailure(nameof(x.ParentId), $"ParentId '{x.PostCategoryTypeId}' not exist");
                                return;
                            }
                            if (parent.PathIds.Contains(x.Id))
                            {
                                context.AddFailure(nameof(x.ParentId), "child cannot be parent");
                                return;
                            }
                        }
                    });
            });

        RuleFor(x => x.PostTypeId)
            .Must(metaModelTypesLocator.ExistPostType)
            .WithMessage(x => $"PostType Id '{x.PostTypeId}' not found");

        RuleFor(x => x.PostCategoryTypeId)
            .Must(postCategoryMetaLocator.ExistType)
            .WithMessage(x => $"PostCategoryType Id '{x.PostCategoryTypeId}' not found");

        RuleFor(x => x.Id)
           .MustAsync(postCategoryRepository.ExistAsync)
           .WithMessage(x => $"PostCategory Id '{x.Id}' not found");

    }
}
