using FluentValidation;
using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Services;
using Mars.Identity.Abstractions.Services;

namespace Mars.Identity.Abstractions.Dto.UserTypes;

public class CreateUserTypeQueryValidator : AbstractValidator<CreateUserTypeQuery>
{
    public CreateUserTypeQueryValidator(IUserMetaLocator userMetaLocator, IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x.TypeName)
            .Must(name => !userMetaLocator.ExistType(name))
            .WithMessage(x => $"User type '{x.TypeName}' already exist");

        RuleFor(x => x).SetValidator(new MetaFieldsDuplicateQueryValidator(metaModelTypesLocator));
    }
}
