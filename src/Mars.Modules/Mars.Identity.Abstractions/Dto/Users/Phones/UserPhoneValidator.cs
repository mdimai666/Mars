using FluentValidation;
using Mars.Contracts.Resources;
using Mars.Identity.Abstractions.Utils;

namespace Mars.Identity.Abstractions.Dto.Users.Phones;

public class UserPhoneValidator : AbstractValidator<string?>
{
    public UserPhoneValidator()
    {
        RuleFor(x => x)
            .Must(x => PhoneUtil.TryNormalizePhone(x, out _))
            .WithMessage(x => $"{AppRes.InvalidPhoneNumberError} '{x}'");
    }
}
