using DynamicExpresso;
using Mars.Core.Models;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Abstractions.Dto;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Connections;
using Mars.Nodes.Expressions;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes.Connections;

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

            Interpreter? interpreter = null;

            if (Node.MessageKind is InputValueKind.Expression or InputValueKind.Msg)
                interpreter = InputValueResolver.CreateInterpreter(RNS, input);

            var resolvedMessage = (string?)InputValueResolver.Resolve(Node.MessageKind, Node.Message, "string", interpreter, new ExpressionScope(RNS, input), Node, "Message");

            var message = string.IsNullOrEmpty(resolvedMessage) ? input.Payload?.ToString()! : resolvedMessage;
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
