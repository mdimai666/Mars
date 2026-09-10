using FluentValidation;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;

namespace Mars.Cms.Abstractions.Dto.PostCategoryTypes;

public class UpdatePostCategoryTypeQueryValidator : AbstractValidator<UpdatePostCategoryTypeQuery>
{
    public UpdatePostCategoryTypeQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator, IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x.TypeName)
            .Must(name => !postCategoryMetaLocator.ExistType(name))
            .When((x) => postCategoryMetaLocator.GetTypeDetailById(x.Id)?.TypeName != x.TypeName)
            .WithMessage(x => $"Post type '{x.TypeName}' already exist");

        RuleFor(x => x).SetValidator(new MetaFieldsDuplicateQueryValidator(metaModelTypesLocator));
    }
}
