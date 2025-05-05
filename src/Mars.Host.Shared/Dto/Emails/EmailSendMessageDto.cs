using System.ComponentModel.DataAnnotations;
using Mars.Core.Utils;

namespace Mars.Host.Shared.Dto.Emails;

public class EmailSendMessageDto : IValidatableObject
{
    public string ToEmail { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }

    public EmailSendMessageDto()
    {
        
    }

    public EmailSendMessageDto(string toemail, string subject, string message)
    {
        this.ToEmail = toemail;
        this.Subject = subject;
        this.Message = message;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        List<ValidationResult> errors = new List<ValidationResult>();

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
