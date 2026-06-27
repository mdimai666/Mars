using Mars.Core.Models;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.Dto;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class DevAdminConnectionNodeImpl : INodeImplement<DevAdminConnectionNode>
{
    public DevAdminConnectionNode Node { get; }
    Node INodeImplement.Node => Node;
    public IRuntimeNodeScope RNS { get; set; }

    public DevAdminConnectionNodeImpl(DevAdminConnectionNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        if (Node.Action == DevAdminConnectionNode.ACTION_MESSAGE)
        {

            var adminConnectionService = RNS.ServiceProvider.GetRequiredService<IDevAdminConnectionService>();

            var message = string.IsNullOrEmpty(Node.Message) ? input.Payload?.ToString()! : Node.Message;
            var messageIntent = Enum.TryParse(Node.MessageIntent, out MessageIntent intent) ? intent : MessageIntent.Info;

            var recepient = Node.MessageRecipient;

            if (recepient == MessageRecipientType.Caller)
            {
                var userId = input.Get<RequestUserInfo>()?.UserId
                                ?? throw new ArgumentException("requestUserInfo not found");

                await adminConnectionService.ShowNotifyMessage(message, userId.ToString(), messageIntent);
            }
            else if (recepient == MessageRecipientType.All)
            {
                await adminConnectionService.ShowNotifyMessageForAll(message.ToString(), messageIntent);
            }
            else throw new NotImplementedException($"MessageRecipient '{recepient}' not implement");

        }
        else
        {
            throw new NotImplementedException($"action '{Node.Action}' not implement");
        }
        RNS.Status(new NodeStatus(DateTime.Now.ToString("HH:mm:ss.fff")));

    }
}
