using FluentValidation;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostTypes;

public class CreatePostTypeQueryValidator : AbstractValidator<CreatePostTypeQuery>
{
    public CreatePostTypeQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x).SetValidator(new GeneralPostTypeQueryValidator());

        RuleFor(x => x.TypeName)
            .Must(name => metaModelTypesLocator.GetPostTypeByName(name) == null)
            .WithMessage(x => $"Post type '{x.TypeName}' already exist");

    }
}
