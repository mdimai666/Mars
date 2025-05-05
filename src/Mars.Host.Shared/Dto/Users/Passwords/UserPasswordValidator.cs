using FluentValidation;

namespace Mars.Host.Shared.Dto.Users.Passwords;

public class UserPasswordValidator : AbstractValidator<string>
{
    public UserPasswordValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .MinimumLength(6)
            .WithMessage("Длина пароля не может быть меньше 6 символов.");
    }
}
