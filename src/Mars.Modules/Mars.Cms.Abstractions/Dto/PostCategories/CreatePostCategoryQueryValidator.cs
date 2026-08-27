using FluentValidation;
using Mars.Core.Features;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Validators;

namespace Mars.Cms.Abstractions.Dto.PostCategories;

public class CreatePostCategoryQueryValidator : AbstractValidator<CreatePostCategoryQuery>
{
    public CreatePostCategoryQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator,
                                            IPostCategoryRepository postCategoryRepository,
                                            IMetaModelTypesLocator metaModelTypesLocator,
                                            IMetaValuesValidator metaValuesValidator)
    {
        RuleFor(x => x.MetaValues).ValidateMetaValues(metaValuesValidator, MetaValueOwnerCatalog.PostCategory, x => x.Id);

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
