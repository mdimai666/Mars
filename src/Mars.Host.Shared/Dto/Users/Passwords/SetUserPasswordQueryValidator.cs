using FluentValidation;

namespace Mars.Host.Shared.Dto.Users.Passwords;

public class SetUserPasswordQueryValidator : AbstractValidator<SetUserPasswordQuery>
{
    public SetUserPasswordQueryValidator()
    {
        RuleFor(x => x.NewPassword).SetValidator(new UserPasswordValidator());

    }
}
