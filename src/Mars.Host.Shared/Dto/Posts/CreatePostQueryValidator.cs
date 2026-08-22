using FluentValidation;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;

namespace Mars.Host.Shared.Dto.Posts;

public class CreatePostQueryValidator : AbstractValidator<CreatePostQuery>
{
    public CreatePostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator,
                                    IPostCategoryRepository postCategoryRepository,
                                    IMetaValuesValidator metaValuesValidator)
    {
        RuleFor(x => x).SetValidator(new GeneralPostQueryValidator(metaModelTypesLocator, postCategoryRepository));

        RuleFor(x => x.MetaValues).ValidateMetaValues(metaValuesValidator);
    }
}
