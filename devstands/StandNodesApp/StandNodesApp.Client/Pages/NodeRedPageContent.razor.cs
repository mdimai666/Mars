using AppFront.Shared.Hub;
using Mars.Core.Models;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core.Nodes.Mappings.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Front.Shared.Services;
using Mars.Nodes.Workspace;
using Microsoft.AspNetCore.Components;

namespace StandNodesApp.Client.Pages;

public partial class NodeRedPageContent
{
    [Inject] INodeServiceClient service { get; set; } = default!;

    [Inject] ClientHub hub { get; set; } = default!;

    NodeEditor1? _editor1 = default!;

    bool Busy = false;

    IDictionary<string, Node>? _nodes;

    IReadOnlyDictionary<string, InlineFunctionNodeSchema> _inlineFunctionNodeSchemas = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        hub.JoinGroup(NodeConstants.WsNodesNotifyGroupName);

        hub.OnNodeStatus += OnNodeStatus;
        hub.OnDebugMsg += OnDebugMsg;
        hub.OnNodeRunningTaskCountChanged += OnNodeRunningTaskCountChanged;
        hub.OnNodeExecuted += OnNodeExecuted;

        hub.ws.Reconnected += OnWsReconnected;

        Load();
    }

    public void Dispose()
    {
        hub.LeaveGroup(NodeConstants.WsNodesNotifyGroupName);

        hub.OnNodeStatus -= OnNodeStatus;
        hub.OnDebugMsg -= OnDebugMsg;
        hub.OnNodeRunningTaskCountChanged -= OnNodeRunningTaskCountChanged;
        hub.OnNodeExecuted -= OnNodeExecuted;

        hub.ws.Reconnected -= OnWsReconnected;
    }

    Task OnWsReconnected(string? connectionId)
    {
        hub.JoinGroup(NodeConstants.WsNodesNotifyGroupName);
        return Task.CompletedTask;
    }

    void OnNodeStatus(string nodeId, NodeStatus nodeStatus)
    {
        var node = _nodes.TryGetValue(nodeId, out var n) ? n : null;
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
            if (true)
            {
                var data = await service.Load();
                var recivedNodes = data.Nodes.ToDictionary(s => s.Id);
                foreach (var state in data.NodesState)
                {
                    var node = recivedNodes[state.Key];
                    node.status = state.Value.Status;
                    node.enable_status = true;
                }
                _nodes = recivedNodes.Values.ToDictionary(s => s.Id);
                _inlineFunctionNodeSchemas = data.InlineFunctionNodeSchemas.ToDictionary(s => s.TypeId, s => s.ToModel());
            }
            else
            {
                var outNode1 = Guid.NewGuid().ToString();
                var outNode2 = Guid.NewGuid().ToString();

                var builder = NodesWorkflowBuilder.Create().AddNext(
                                NodesWorkflowBuilder.Create()
                                    .AddNext(new InjectNode())
                                    .AddNext(new LinkInNode() { OutLinksIds = [outNode1, outNode2] }),
                                NodesWorkflowBuilder.Create()
                                    .AddNext(new LinkOutNode() { Id = outNode1 })
                                    .AddNext(new DebugNode(), new DebugNode()),
                                NodesWorkflowBuilder.Create()
                                    .AddNext(new LinkOutNode() { Id = outNode2 })
                                    .AddNext(new DebugNode())
                                );

                _nodes = builder.BuildWithFlowNode().ToDictionary(s => s.Id);

                _inlineFunctionNodeSchemas = new Dictionary<string, InlineFunctionNodeSchema>();
            }
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
            foreach (var node in _nodes.Values) node.changed = false;
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
