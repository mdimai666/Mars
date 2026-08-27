using FluentValidation;
using Mars.Identity.Abstractions.Utils;
using Mars.Contracts.Resources;

namespace Mars.Identity.Abstractions.Dto.Users.Phones;

public class UserPhoneValidator : AbstractValidator<string?>
{
    public UserPhoneValidator()
    {
        RuleFor(x => x)
            .Must(x => PhoneUtil.TryNormalizePhone(x, out _))
            .WithMessage(x=>$"{AppRes.InvalidPhoneNumberError} '{x}'");
    }
}
