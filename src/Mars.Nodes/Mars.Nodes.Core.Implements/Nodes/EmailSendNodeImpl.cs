using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.Dto.Emails;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes;
using Mars.Shared.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class EmailSendNodeImpl : INodeImplement<EmailSendNode>, INodeImplement
{

    public EmailSendNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public EmailSendNodeImpl(EmailSendNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;

        Node.Config = RED.GetConfig(node.Config);
    }

    public Task Execute(NodeMsg input, ExecuteAction callback)
    {
        var mailSender = RED.ServiceProvider.GetRequiredService<IMarsEmailSender>();
        var opt = RED.ServiceProvider.GetRequiredService<IOptionService>();

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
            //info = new EmailSendMessageDto
            //{
            //    ToEmail = Node.ToEmail,
            //    Subject = Node.Subject,
            //    Message = input.Payload?.ToString() ?? "",
            //};
            //info = ((EmailSendMessageDto)input.Payload).CopyViaJsonConversion<EmailSendMessageDto>();
        }

        info ??= new();

        if (!string.IsNullOrWhiteSpace(Node.ToEmail)) { info.ToEmail = Node.ToEmail; }
        //if (!string.IsNullOrWhiteSpace(Node.Message)) { info.Message = Node.Message; }
        if (!string.IsNullOrWhiteSpace(Node.Subject)) { info.Subject = Node.Subject; }

        var valid = info.Validate(new ValidationContext(info));

        if (valid.Any())
        {
            //RED.DebugMsg(new DebugMessage
            //{
            //    message = valid
            //});
            throw new Exception(string.Join("; ", valid));
        }

        RED.Status(new NodeStatus { Text = "request...", Color = "blue" });


        //var info = input.Get<EmailSendInfo>();

        mailSender.SendEmailForce(info.ToEmail, smtp.Username, info.Subject, info.Message, html: true, Convert(smtp));

        RED.Status(new NodeStatus { Text = "complete", Color = "blue" });

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
