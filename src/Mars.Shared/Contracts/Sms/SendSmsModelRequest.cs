using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Mars.Shared.Common;

namespace Mars.Shared.Contracts.Sms;

public class SendSmsModelRequest : IValidatableObject
{
    [Required, Display(Name = "Мобильный телефон")]
    [DataType(DataType.PhoneNumber)]
    [Phone]
    public required string Phone { get; set; }

    [Required, Display(Name = "Сообщение")]
    [DataType(DataType.MultilineText)]
    public required string Message { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        List<ValidationResult> errors = new List<ValidationResult>();

        if (IsValidPhoneNumber(Phone) == false)
        {
            errors.Add(new ValidationResult(
                "Неправильный формат номера", new string[] { nameof(Phone) })
            );
        }

        if (string.IsNullOrWhiteSpace(Message))
        {
            errors.Add(new ValidationResult(
                "Нельзя отправить пустое сообщение", new string[] { nameof(Message) })
            );
        }

        return errors;
    }

    public static bool IsValidPhoneNumber(string This)
    {
        var phoneNumber = This.Trim()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "");
        return Regex.Match(phoneNumber, @"^\+\d{5,15}$").Success;
    }

    public bool IsValid()
    {
        ValidationContext validationContext = new(this);
        var errors = Validate(validationContext);

        bool isValid = errors.Count() == 0;

        return isValid;
    }

    public UserActionResult? EnsureValidate(SendSmsModelRequest form)
    {
        ValidationContext validationContext = new(form);
        var errors = form.Validate(validationContext);

        bool isValid = errors.Count() == 0;

        if (isValid == false) return new UserActionResult
        {
            Message = string.Join('\n', errors.Select(s => s.ErrorMessage).ToList())
        };

        return null;
    }
}
