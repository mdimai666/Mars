using System.ComponentModel.DataAnnotations;
using Mars.Core.Utils;

namespace Mars.Notifications.Contracts;

public class EmailSendMessageDto : IValidatableObject
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public EmailSendMessageDto()
    {

    }

    public EmailSendMessageDto(string toemail, string subject, string message)
    {
        ToEmail = toemail;
        Subject = subject;
        Message = message;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        List<ValidationResult> errors = [];

        if (string.IsNullOrWhiteSpace(Message))
            errors.Add(new ValidationResult("Сообщение не может быть пустым"));

        if (string.IsNullOrWhiteSpace(ToEmail))
            errors.Add(new ValidationResult("email не может быть пустым"));

        if (!EmailUtil.IsEmail(ToEmail))
        {
            errors.Add(new ValidationResult("Невалидный email"));
        }

        return errors;

    }
}
