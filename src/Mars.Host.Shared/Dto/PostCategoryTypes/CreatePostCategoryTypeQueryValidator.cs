using FluentValidation;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public class CreatePostCategoryTypeQueryValidator : AbstractValidator<CreatePostCategoryTypeQuery>
{
    public CreatePostCategoryTypeQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator)
    {
        RuleFor(x => x.TypeName)
            .Must(name => !postCategoryMetaLocator.ExistType(name))
            .WithMessage(x => $"Post type '{x.TypeName}' already exist");

        RuleFor(x => x).SetValidator(new MetaFieldsDuplicateQueryValidator());
    }
}
