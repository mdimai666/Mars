using Mars.Shared.Common;
using Mars.Shared.Options;

namespace Mars.Host.Shared.Services;

public interface IMarsEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage);
    public Task SendEmailForce(string to_email, string from_name, string subject, string message, bool html = false, SmtpSettingsModel? smtpSettings = null);
    public Task<UserActionResult> SendTestEmail(TestMailMessage form);
}
