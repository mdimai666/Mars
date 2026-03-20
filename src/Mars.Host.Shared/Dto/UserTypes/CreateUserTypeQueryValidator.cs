using FluentValidation;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.UserTypes;

public class CreateUserTypeQueryValidator : AbstractValidator<CreateUserTypeQuery>
{
    public CreateUserTypeQueryValidator(IUserMetaLocator userMetaLocator)
    {
        RuleFor(x => x.TypeName)
            .Must(name => !userMetaLocator.ExistType(name))
            .WithMessage(x => $"User type '{x.TypeName}' already exist");

        RuleFor(x => x).SetValidator(new MetaFieldsDuplicateQueryValidator());
    }
}
