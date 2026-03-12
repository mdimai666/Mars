using Mars.Nodes.Core;
using Microsoft.AspNetCore.SignalR;

namespace Mars.Host.Shared.Hubs;

/// <summary>
/// Broadcast server
/// </summary>
public class BroadcastHub
{
    private readonly IHubContext<ChatHub> _hub;

    IClientProxy _nodesClients => _hub.Clients.Group(NodeConstants.WsNodesNotifyGroupName);

    public BroadcastHub(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public void DebugMsg(string nodeId, DebugMessage msg)
    {
        _nodesClients.SendAsync("DebugMsg", nodeId, msg);
    }

    public void DebugMsg(string nodeId, Exception ex)
    {
        _nodesClients.SendAsync("DebugMsg", nodeId, new DebugMessage
        {
            NodeId = nodeId,
            Message = ex.Message,
            Level = Mars.Core.Models.MessageIntent.Error,
        });
    }

    public void NodeStatus(string nodeId, NodeStatus nodeStatus)
    {
        _nodesClients.SendAsync("NodeStatus", nodeId, nodeStatus);
    }

    public void NodeRunningTaskCountChanged(int nodeRunningTaskCount)
    {
        _nodesClients.SendAsync("NodeRunningTaskCountChanged", nodeRunningTaskCount);
    }

    public void OnNodeExecuted(Guid taskId, string nodeId, NodeExecutionTrigger trigger)
    {
        _nodesClients.SendAsync("OnNodeExecuted", taskId, nodeId, trigger);
    }
}
