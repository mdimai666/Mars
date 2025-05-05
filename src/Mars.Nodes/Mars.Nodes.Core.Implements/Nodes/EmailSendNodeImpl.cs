using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes;
using Mars.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Mars.Shared.Options;
using Mars.Host.Shared.Dto.Emails;

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
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {

        try
        {
            var mailSender = RED.ServiceProvider.GetRequiredService<IMarsEmailSender>();
            var opt = RED.ServiceProvider.GetRequiredService<IOptionService>();

            var smtp = opt.GetOption<SmtpSettingsModel>();

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
                info = ((EmailSendMessageDto)input.Payload).CopyViaJsonConversion<EmailSendMessageDto>();
            }

            info ??= new();

            if (!string.IsNullOrWhiteSpace(Node.ToEmail)) { info.ToEmail = Node.ToEmail; }
            if (!string.IsNullOrWhiteSpace(Node.Message)) { info.Message = Node.Message; }
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

            mailSender.SendEmailForce(info.ToEmail, smtp.FromName, info.Subject, info.Message, html: true);

            RED.Status(new NodeStatus { Text = "complete", Color = "blue" });


            callback(input);

        }
        catch (Exception ex)
        {
            RED.Status(new NodeStatus { Text = "error" });
            RED.DebugMsg(ex);
        }

        return Task.CompletedTask;
    }

}
