using System.Text.RegularExpressions;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Users;

public class UserNameRuleValidator : AbstractValidator<string>
{
    private static readonly Regex AllowedChars = new(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

    public UserNameRuleValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Username cannot be empty.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(30).WithMessage("Username must not exceed 30 characters.")
            .Must(v => AllowedChars.IsMatch(v))
                .WithMessage("Username can only contain letters, numbers, '.', '_' or '-'.")
            .Must(v => !char.IsDigit(v[0]))
                .WithMessage("Username cannot start with a digit.")
            .Must(v => !v.All(char.IsDigit))
                .WithMessage("Username cannot consist only of digits.");

    }
}
