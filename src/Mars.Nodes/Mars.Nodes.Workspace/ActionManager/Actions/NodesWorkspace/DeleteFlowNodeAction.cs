using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class DeleteFlowNodeAction : BaseEditorHistoryAction
{
    private readonly string _flowNodeId;
    private readonly string _delNodesJson;
    private readonly string[] _delNodesIds;

    public DeleteFlowNodeAction(INodeEditorApi editor, string flowNodeId) : base(editor)
    {
        _flowNodeId = flowNodeId;
        var delNodes = _editor.GetFlowNodes(flowNodeId).Values.Concat([_editor.AllNodes[flowNodeId]]);
        _delNodesJson = NodesToJson(delNodes);
        _delNodesIds = delNodes.Select(s => s.Id).ToArray();
    }

    public override bool CanExecute() => true;

    public override void Execute()
    {
        //var flows = _editor.AllNodes.Values.Where(s => s is FlowNode).Select(s => s as FlowNode).ToList();
        var flowsCount = _editor.AllNodes.Values.Count(s => s is FlowNode);
        var targetFlow = (FlowNode)_editor.AllNodes[_flowNodeId];

        if (flowsCount == 1)
        {
            var flow = new FlowNode() { Name = "flow " + flowsCount };
            _editor.AddNodes([flow]);
            //flows = _editor.AllNodes.Values.Where(s => s is FlowNode).Select(s => s as FlowNode).ToList();
        }

        SwitchNextActuallyFlow(_editor, _flowNodeId);

        var delNodes = _delNodesIds.Select(s => _editor.AllNodes[s]);

        _editor.DeleteNodes(delNodes);
    }

    public override void Undo()
    {
        var flowsCountWas = _editor.AllNodes.Values.Count(s => s is FlowNode);

        var createNodes = NodesFromJson(_delNodesJson);
        _editor.AddNodes(createNodes);
        var targetFlow = (FlowNode)_editor.AllNodes[_flowNodeId];
        _editor.ChangeFlow(targetFlow);

        if (flowsCountWas == 1)
        {
            var toDelFlow = _editor.AllNodes.Values.First(s => s is FlowNode && s.Id != _flowNodeId);
            _editor.DeleteNodes([toDelFlow]);
        }
    }

    public static void SwitchNextActuallyFlow(INodeEditorApi _editor, string targerFlowId)
    {
        var flows = _editor.AllNodes.Values.Where(s => s is FlowNode).Select(s => s as FlowNode).ToList();
        var targetFlow = (FlowNode)_editor.AllNodes[targerFlowId];
        var index = flows.IndexOf(targetFlow);
        var nextFlow = flows.ElementAtOrDefault(index + 1) ?? flows.Last(s => s != targetFlow)!;
        _editor.ChangeFlow(nextFlow);
    }
}
