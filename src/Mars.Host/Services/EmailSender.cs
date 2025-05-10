using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.ActionHistorys;
using Mars.Shared.Options;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace Mars.Host.Services;

internal class EmailSender : IEmailSender, IMarsEmailSender
{
    private readonly IActionHistoryService _actionHistoryService;
    private readonly IOptionService _optionService;
    private SmtpSettingsModel _smtpSettings;

    public EmailSender(IActionHistoryService actionHistoryService, IOptionService optionService)
    {
        _actionHistoryService = actionHistoryService;
        _optionService = optionService;
        _smtpSettings = _optionService.MailSettings;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        if (_smtpSettings.IsTestServer)
        {
            Console.WriteLine("Test server: Email not sent");
            return;
        }
        await SendEmailForce(email, _smtpSettings.FromName, subject, message);
    }

    public async Task SendEmailForce(string to_email, string from_name, string subject, string message, bool html = false, SmtpSettingsModel? smtpSettings = null)
    {
        smtpSettings ??= _smtpSettings;

        //try
        //{
        string from = smtpSettings.SmtpUser;
        string host = smtpSettings.Host;
        int port = smtpSettings.Port;
        bool ssl = smtpSettings.Secured;

        {
            var source = new MailboxAddress(from_name, from);
            var email = new MimeMessage();
            email.From.Add(source);
            email.ReplyTo.Add(source);
            email.Sender = source;

            //email.To.Add(new MailboxAddress(from_name, to_email));
            email.To.Add(new MailboxAddress("", to_email));

            //Recipients.ForEach(recipient => email.To.Add(new MailboxAddress(recipient.DisplayName, recipient.Address)));

            //if (CC != null)
            //{
            //    CC.ForEach(cc => email.Cc.Add(new MailboxAddress(cc.DisplayName, cc.Address)));
            //}

            email.Subject = subject;
            email.Body = new TextPart(html ? "html" : "plain") { Text = message };

            //Limilabs.Mail.IMail email = builder.Create();

            //using (var client = new SmtpClient(SMTPServerAddress, SMTPRemotePort))
            using (var smtp = new MailKit.Net.Smtp.SmtpClient())
            {
                smtp.Connect(host, port, ssl);

                if (true)
                {
                    smtp.Authenticate(from, smtpSettings.SmtpPassword);
                }

                try
                {
                    await _actionHistoryService.Add(email, $"Send email - {to_email}", ActionHistoryLevel.Info, "email");
                    await smtp.SendAsync(email);
                }
                catch (Exception ex)
                {
                    _actionHistoryService.Add(ex, "Email cant sent").Wait();
                }

            }
        }
        //}
        //catch (Exception ex)
        //{
        //    ExceptionPolicy.HandleException(ex, "Default Policy");

        //    var fault = new EmailSendingFailureFault
        //    {
        //        Message = ex.Message,
        //        Description = ex.ToString()
        //    };

        //    throw new FaultException<EmailSendingFailureFault>(fault, "Failure sending mail");
        //}
    }

#if FALSE
    public MailAddress From { get; set; }
    public MailAddress Sender { get; set; }

    public List<MailAddress> Recipients { get; set; }
    public List<MailAddress> CC { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsBodyHTML { get; set; }
    public List<MailAttachmentInfo> Attachments { get; set; }
    public List<MailAttachmentInfo> LinkedAttachments { get; set; }

    public void SendEmail(bool isTest, string testEmail)
    {



        var From = new MailboxAddress(this.From.DisplayName, this.From.Address);
        var Recipients = this.Recipients.Select(s => new MailboxAddress(s.DisplayName, s.Address)).ToList();

        var email = new MimeMessage();

        email.From.Add(isTest ? new MailboxAddress("Test Recipient", testEmail) : From);
        email.Sender = isTest ? new MailboxAddress("Test Recipient", testEmail) : From;
        email.ReplyTo.Add(isTest ? new MailboxAddress("Test Recipient", testEmail) : From);

        Recipients.ForEach(recipient => email.To.Add(isTest ? new MailboxAddress("Test Recipient", testEmail) : recipient));

        if (CC != null)
        {
            CC.ForEach(cc => email.Cc.Add(isTest ? new MailboxAddress("Test Recipient", testEmail) : new MailboxAddress(cc.DisplayName, cc.Address)));
        }

        email.Subject = (Subject);

        string body = Body;
        if (IsBodyHTML)
        {
            var pm = new PreMailer();
            body = pm.MoveCssInline(body);
        }
        //email.IsBodyHtml = IsBodyHTML;

        var builder = new BodyBuilder();


        if (IsBodyHTML && LinkedAttachments != null && LinkedAttachments.Count > 0)
        {
            //var plainView = AlternateView.CreateAlternateViewFromString("To view the content of this message, please use an HTML enabled mail client.", Encoding.UTF8, MediaTypeNames.Text.Plain);
            //var htmlView = AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, MediaTypeNames.Text.Html);

            foreach (var mailAttachmentInfo in LinkedAttachments)
            {
                //var ms = new MemoryStream(mailAttachmentInfo.ContentBytes);
                //var resource = new LinkedResource(ms, mailAttachmentInfo.MimeType) { ContentId = mailAttachmentInfo.ContentId };
                //htmlView.LinkedResources.Add(resource);

                var resource = builder.LinkedResources.Add(mailAttachmentInfo.FileName);
                resource.ContentId = mailAttachmentInfo.ContentId;

            }

            if (Attachments != null && Attachments.Count > 0)
            {
                foreach (MailAttachmentInfo mailAttachmentInfo in Attachments)
                {
                    //var ms = new MemoryStream(mailAttachmentInfo.ContentBytes);
                    //var attachment = new Attachment(ms, mailAttachmentInfo.FileName, mailAttachmentInfo.MimeType);
                    //email.Attachments.Add(attachment);
                    builder.Attachments.Add(mailAttachmentInfo.FileName);
                }
            }

            //email.AlternateViews.Add(plainView);
            //email.AlternateViews.Add(htmlView);

            builder.TextBody = "To view the content of this message, please use an HTML enabled mail client.";
            builder.HtmlBody = body;

            email.Body = builder.ToMessageBody();

        }
        else
            email.Body = new TextPart("plain") { Text = body };



        var smtp = new MailKit.Net.Smtp.SmtpClient();

        smtp.Connect(SMTPServerAddress, SMTPRemotePort, SMTPServerRequiresSecureConnection);

        if (SMTPServerRequiresAuthentication)
        {
            smtp.Authenticate(Sender.Address, SmtpServerPassword);
        }
        else
        {
        }




        smtp.MessageSent += delegate (object sender, MailKit.MessageSentEventArgs e)
        {


            //client.SendCompleted += delegate (object sender, AsyncCompletedEventArgs e)
            //{
            //if (e.Error != null)
            //{
            //    var error = new StringBuilder();
            //    error.AppendLine("Unable to send email");
            //    error.AppendLine(string.Format("To: {0}", email.To[0].Address));
            //    error.AppendLine(string.Format("Subject: {0}", email.Subject));
            //    error.AppendLine(e.Error.ToString());
            //    System.Diagnostics.Trace.TraceError(error.ToString());

            //    Exception exception = new ApplicationException(error.ToString());
            //    ExceptionPolicy.HandleException(exception, "Default Policy");
            //}

            //email.Dispose();
        };

        //client.SendAsync(email, null);
        smtp.SendAsync(email);
    } 
#endif

    public async Task<UserActionResult> SendTestEmail(TestMailMessage form)
    {
        try
        {
            await SendEmailForce(form.Email, _smtpSettings.FromName, form.Subject, form.Message);

            return new UserActionResult
            {
                Ok = true,
                Message = "Успешно отправлено"
            };
        }
        catch (Exception ex)
        {
            return new UserActionResult
            {
                Ok = false,
                Message = ex.Message
            };
        }
    }
}
