using AppFront.Shared.Hub;
using Mars.Core.Models;
using Mars.Nodes.Core;
using Mars.Nodes.Front.Shared.Services;
using Mars.Nodes.Workspace;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Builder.NodeViews;

public partial class NodeRedPage
{
    [Inject] INodeServiceClient service { get; set; } = default!;

    [Inject] ClientHub hub { get; set; } = default!;

    NodeEditor1? _editor1 = default!;

    bool Busy = false;

    IDictionary<string, Node>? nodes;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        hub.JoinGroup(NodeConstants.WsNodesNotifyGroupName);

        hub.OnNodeStatus += OnNodeStatus;
        hub.OnDebugMsg += OnDebugMsg;
        hub.OnNodeRunningTaskCountChanged += OnNodeRunningTaskCountChanged;
        hub.OnNodeExecuted += OnNodeExecuted;

        Load();
    }

    public void Dispose()
    {
        hub.LeaveGroup(NodeConstants.WsNodesNotifyGroupName);

        hub.OnNodeStatus -= OnNodeStatus;
        hub.OnDebugMsg -= OnDebugMsg;
        hub.OnNodeRunningTaskCountChanged -= OnNodeRunningTaskCountChanged;
        hub.OnNodeExecuted -= OnNodeExecuted;
    }

    void OnNodeStatus(string nodeId, NodeStatus nodeStatus)
    {
        var node = nodes.TryGetValue(nodeId, out var n) ? n : null;
        if (node is not null)
        {
            node.enable_status = string.IsNullOrEmpty(nodeStatus.Text) == false;
            node.status = nodeStatus.Text;
        }
        StateHasChanged();
    }

    void OnDebugMsg(string nodeId, DebugMessage msg)
    {
        _editor1?.AddDebugMessage(msg);
    }

    void OnNodeRunningTaskCountChanged(int currentTaskCount)
    {
        _editor1?.SetCurrentTaskCount(currentTaskCount);
    }

    void OnNodeExecuted(Guid taskId, string nodeId, NodeExecutionTrigger trigger)
    {
        _editor1?.CallNodeInjectedEffect(taskId, nodeId, trigger);
    }

    async void Load()
    {
        Busy = true;
        StateHasChanged();

        try
        {
            var data = await service.Load();
            var recivedNodes = data.Nodes.ToDictionary(s => s.Id);
            foreach (var state in data.NodesState)
            {
                var node = recivedNodes[state.Key];
                node.status = state.Value.Status;
                node.enable_status = true;
            }
            nodes = recivedNodes.Values.ToDictionary(s => s.Id);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }

        Busy = false;
        StateHasChanged();

    }

    async void OnDeploy(IEnumerable<Node> Nodes)
    {
        var res = await service.Deploy(Nodes);

        if (res.Ok)
        {
            foreach (var node in nodes.Values) node.changed = false;
        }

        _editor1!.AddDebugMessage(new DebugMessage
        {
            // Topic = (res.Ok ? "OK" : "FAIL"),
            Message = res.Message,
            Level = res.Ok ? MessageIntent.Info : MessageIntent.Error
        });
    }

    async void OnInjectClick(string nodeId)
    {
        bool useWs = true;
        //Console.WriteLine($">Click Inject:{nodeId}");

        if (useWs)
        {
            hub.Inject(nodeId);
        }
        else
        {
            var res = await service.Inject(nodeId);
            string ok = (res.Ok ? "OK" : "FAIL");
        }
    }

    void OnCmdClick(string cmd)
    {
        hub.SendMessage("other");
    }

}
