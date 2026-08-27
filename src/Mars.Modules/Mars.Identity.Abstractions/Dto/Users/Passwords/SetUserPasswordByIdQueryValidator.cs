using FluentValidation;

namespace Mars.Host.Shared.Dto.Users.Passwords;

public class SetUserPasswordByIdQueryValidator : AbstractValidator<SetUserPasswordByIdQuery>
{
    public SetUserPasswordByIdQueryValidator()
    {
        RuleFor(x => x.NewPassword).SetValidator(new UserPasswordValidator());

    }
}
