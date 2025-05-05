using System.ComponentModel.DataAnnotations;

namespace Mars.Core.Attributes;

public class EmailAddressThatAllowsBlanks : ValidationAttribute
{
    public const string DefaultErrorMessage = "{0} must be a valid email address";
    private EmailAddressAttribute _validator = new EmailAddressAttribute();

    public EmailAddressThatAllowsBlanks() : base(DefaultErrorMessage)
    {

    }

    public override bool IsValid(object? value)
    {
        if (string.IsNullOrEmpty(value?.ToString()))
            return true;

        return _validator.IsValid(value.ToString());
    }
}
