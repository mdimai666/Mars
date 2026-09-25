using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Network;
using Mars.Nodes.Expressions;
using Mars.Notifications.Abstractions;
using Mars.Notifications.Contracts;
using Mars.Options.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes.Network;

public class EmailSendNodeImpl : INodeImplement<EmailSendNode>
{

    public EmailSendNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public EmailSendNodeImpl(EmailSendNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;

        Node.Config = RNS.GetConfig(node.Config);
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var mailSender = RNS.ServiceProvider.GetRequiredService<IMarsEmailSender>();
        var opt = RNS.ServiceProvider.GetRequiredService<IOptionService>();

        //var smtp = opt.GetOption<SmtpSettingsModel>();

        var smtp = Node.Config.Value;

        EmailSendMessageDto? info = null;
        if (input.Payload is null)
        {

        }
        else if (input.Payload is EmailSendMessageDto dto)
        {
            info = dto;
        }
        else
        {
            info = new EmailSendMessageDto { Message = input.Payload.ToString() ?? "" };
        }

        var (toEmail, subject, message) = ResolveFields();

        info ??= new();

        if (!string.IsNullOrWhiteSpace(toEmail)) { info.ToEmail = toEmail!; }
        if (!string.IsNullOrWhiteSpace(message)) { info.Message = message!; }
        if (!string.IsNullOrWhiteSpace(subject)) { info.Subject = subject!; }

        // аренда runner'а только на резолвинг — SMTP-отправка ниже идёт вне сессии
        (string? To, string? Subject, string? Message) ResolveFields()
        {
            using var expr = RNS.Expressions(Node);

            string? Resolve(string kind, string value, string source)
                => (string?)expr.Resolve(kind, value, "string", input, Node, source);

            return (Resolve(Node.ToEmailKind, Node.ToEmail, "ToEmail"),
                    Resolve(Node.SubjectKind, Node.Subject, "Subject"),
                    Resolve(Node.MessageKind, Node.Message, "Message"));
        }

        var valid = info.Validate(new ValidationContext(info));

        if (valid.Any())
        {
            //RNS.DebugMsg(new DebugMessage
            //{
            //    message = valid
            //});
            throw new Exception(string.Join("; ", valid));
        }

        RNS.Status(new NodeStatus { Text = "request...", Color = "blue" });

        //var info = input.Get<EmailSendInfo>();

        mailSender.SendEmailForce(info.ToEmail, smtp.Username, info.Subject, info.Message, html: true, Convert(smtp));

        RNS.Status(new NodeStatus { Text = "complete", Color = "blue" });

        callback(input);
        return Task.CompletedTask;
    }

    SmtpSettingsModel Convert(SmtpConfigNode config)
        => new()
        {
            FromName = config.Username,
            Host = config.ServerName,
            IsTestServer = false,
            Port = config.Port,
            Secured = config.SSLRequired,
            SmtpUser = config.SMTPAuthenticationRequired ? config.Username : "",
            SmtpPassword = config.SMTPAuthenticationRequired ? config.Password : "",
        };
}
