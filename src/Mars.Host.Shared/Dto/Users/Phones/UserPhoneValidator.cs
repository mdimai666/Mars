using FluentValidation;
using Mars.Host.Shared.Utils;
using Mars.Shared.Resources;

namespace Mars.Host.Shared.Dto.Users.Phones;

public class UserPhoneValidator : AbstractValidator<string?>
{
    public UserPhoneValidator()
    {
        RuleFor(x => x)
            .Must(x => PhoneUtil.TryNormalizePhone(x, out _))
            .WithMessage(AppRes.InvalidPhoneNumberError);
    }
}
