using FluentValidation;

namespace Mars.Identity.Abstractions.Dto.Users.Passwords;

public class SetUserPasswordQueryValidator : AbstractValidator<SetUserPasswordQuery>
{
    public SetUserPasswordQueryValidator()
    {
        RuleFor(x => x.NewPassword).SetValidator(new UserPasswordValidator());

    }
}
