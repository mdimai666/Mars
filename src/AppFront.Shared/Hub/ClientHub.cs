using AppFront.Shared.Interfaces;
using Mars.Nodes.Core;
using Microsoft.AspNetCore.SignalR.Client;

namespace AppFront.Shared.Hub;

public class ClientHub : IClientHub
{
    public readonly HubConnection ws;

    public ClientHub(HubConnection ws)
    {
        ws.On("Event", (string id, object payload) =>
        {
            Console.WriteLine($">Event: {id}; {payload}");
        });

        ws.On("ReceiveMessage", (string user, object message) =>
        {
            Console.WriteLine($"<<= ReceiveMessage: {user}; {message}");
        });

        ws.On("DebugMsg", (Action<string, DebugMessage>)((string nodeId, DebugMessage msg) =>
        {
            Console.WriteLine($"<<= DebugMsg:{nodeId}; {msg?.Message}");
            OnDebugMsg?.Invoke(nodeId, msg!);
        }));

        ws.On("NodeStatus", (string nodeId, NodeStatus nodeStatus) =>
        {
            Console.WriteLine($"<<= NodeStatus:{nodeId}; {nodeStatus}");
            OnNodeStatus?.Invoke(nodeId, nodeStatus!);
        });

        ws.On("ShowNotifyMessage", (string message, Mars.Core.Models.MessageIntent messageIntent) =>
        {
            Console.WriteLine($"<<= ShowNotifyMessage:{message}; {messageIntent}");
            OnShowNotifyMessage?.Invoke(message, messageIntent);
        });

        ws.On("NodeRunningTaskCountChanged", (int taskCount) =>
        {
            Console.WriteLine($"<<= NodeRunningTaskCountChanged:{taskCount}");
            OnNodeRunningTaskCountChanged?.Invoke(taskCount);
        });

        ws.On("OnNodeExecuted", (Guid taskId, string nodeId, NodeExecutionTrigger trigger) =>
        {
            Console.WriteLine($"<<= taskId:{taskId}, nodeId:{nodeId}, trigger:{trigger}");
            OnNodeExecuted?.Invoke(taskId, nodeId, trigger);
        });

        this.ws = ws;
    }

    public void SendMessage(string message)
    {
        ws.SendAsync("SendMessage", "red", message);
    }

    public void Inject(string nodeId)
    {
        ws.SendAsync("Inject", nodeId);
    }

    public event Action<string, NodeStatus> OnNodeStatus = default!;
    public event Action<string, DebugMessage> OnDebugMsg = default!;
    public event Action<string, Mars.Core.Models.MessageIntent> OnShowNotifyMessage = default!;
    public event ClietHubNodeTaskExecutionHandler OnNodeExecuted = default!;

    public event Action<int> OnNodeRunningTaskCountChanged = default!;

    public Task JoinGroup(string groupName)
    {
        return ws.InvokeAsync("JoinGroup", groupName);
    }

    public Task LeaveGroup(string groupName)
    {
        return ws.InvokeAsync("LeaveGroup", groupName);
    }
}

public delegate void ClietHubNodeTaskExecutionHandler(Guid taskId, string nodeId, NodeExecutionTrigger trigger);
