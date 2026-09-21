using System.ComponentModel.DataAnnotations;
using DynamicExpresso;
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

        var interpreter = NeedInterpreter() ? InputValueResolver.CreateInterpreter(RNS, input) : null;
        var scope = new ExpressionScope(RNS, input);

        var toEmail = Resolve(Node.ToEmailKind, Node.ToEmail, "ToEmail");
        var subject = Resolve(Node.SubjectKind, Node.Subject, "Subject");
        var message = Resolve(Node.MessageKind, Node.Message, "Message");

        info ??= new();

        if (!string.IsNullOrWhiteSpace(toEmail)) { info.ToEmail = toEmail!; }
        if (!string.IsNullOrWhiteSpace(message)) { info.Message = message!; }
        if (!string.IsNullOrWhiteSpace(subject)) { info.Subject = subject!; }

        bool NeedInterpreter()
            => Node.ToEmailKind is InputValueKind.Expression or InputValueKind.Msg
               || Node.SubjectKind is InputValueKind.Expression or InputValueKind.Msg
               || Node.MessageKind is InputValueKind.Expression or InputValueKind.Msg;

        string? Resolve(string kind, string value, string source)
            => (string?)InputValueResolver.Resolve(kind, value, "string", interpreter, scope, Node, source);

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
