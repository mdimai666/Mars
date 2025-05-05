using System.Text.RegularExpressions;
using Mars.Shared.Resources;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Users.Phones;

public class UserPhoneValidator : AbstractValidator<string?>
{
    //[GeneratedRegex(@"^\+?\d{3,12}$")]
    public static readonly Regex PhoneRegex = new Regex(@"^\+?\d{3,12}$");

    public UserPhoneValidator()
    {
        RuleFor(x => x)
            .Matches(PhoneRegex)
            .WithMessage(AppRes.InvalidPhoneNumberError);
    }
}
