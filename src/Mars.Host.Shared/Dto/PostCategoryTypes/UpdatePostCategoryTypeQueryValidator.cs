using FluentValidation;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostCategoryTypes;

public class UpdatePostCategoryTypeQueryValidator : AbstractValidator<UpdatePostCategoryTypeQuery>
{
    public UpdatePostCategoryTypeQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator)
    {
        RuleFor(x => x.TypeName)
            .Must(name => !postCategoryMetaLocator.ExistType(name))
            .When((x) => postCategoryMetaLocator.GetTypeDetailById(x.Id)?.TypeName != x.TypeName)
            .WithMessage(x => $"Post type '{x.TypeName}' already exist");

        RuleFor(x => x).SetValidator(new MetaFieldsDuplicateQueryValidator());
    }
}
