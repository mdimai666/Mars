using Mars.Host.Shared.Services;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Posts;

public class CreatePostQueryValidator : AbstractValidator<CreatePostQuery>
{
    public CreatePostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x).SetValidator(new GeneralPostQueryValidator(metaModelTypesLocator));
    }
}
