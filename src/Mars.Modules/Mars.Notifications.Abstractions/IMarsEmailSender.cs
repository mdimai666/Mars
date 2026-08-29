using Mars.Contracts.Common;

namespace Mars.Notifications.Abstractions;

public interface IMarsEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage);
    public Task SendEmailForce(string to_email, string from_name, string subject, string message, bool html = false, SmtpSettingsModel? smtpSettings = null);
    public Task<UserActionResult> SendTestEmail(TestMailMessage form);
}
