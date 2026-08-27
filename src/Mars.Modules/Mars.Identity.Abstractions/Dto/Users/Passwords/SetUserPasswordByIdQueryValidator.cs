using FluentValidation;

namespace Mars.Identity.Abstractions.Dto.Users.Passwords;

public class SetUserPasswordByIdQueryValidator : AbstractValidator<SetUserPasswordByIdQuery>
{
    public SetUserPasswordByIdQueryValidator()
    {
        RuleFor(x => x.NewPassword).SetValidator(new UserPasswordValidator());

    }
}
