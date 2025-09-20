using Mars.Core.Models;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class DevAdminConnectionNodeImpl : INodeImplement<DevAdminConnectionNode>, INodeImplement
{
    public DevAdminConnectionNode Node { get; }
    Node INodeImplement<Node>.Node => Node;
    public IRED RED { get; set; }

    public DevAdminConnectionNodeImpl(DevAdminConnectionNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        if (Node.Action == DevAdminConnectionNode.ACTION_MESSAGE)
        {

            var adminConnectionService = RED.ServiceProvider.GetRequiredService<IDevAdminConnectionService>();

            var message = string.IsNullOrEmpty(Node.Message) ? input.Payload?.ToString()! : Node.Message;
            var messageIntent = Enum.TryParse(Node.MessageIntent, out MessageIntent intent) ? intent : MessageIntent.Info;

            await adminConnectionService.ShowNotifyMessage(message, messageIntent);
        }
        else
        {
            throw new NotImplementedException($"action '{Node.Action}' not implement");
        }
        RED.Status(new NodeStatus(DateTime.Now.ToString("HH:mm:ss.fff")));

    }
}
