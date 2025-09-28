using Mars.Nodes.Core.Nodes;
using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

[EditorActionCommand("CreateFlowNode", "Alt+KeyF")]
public class CreateFlowNodeAction : BaseEditorHistoryAction
{
    private string? _addedFlowNodeId;

    public CreateFlowNodeAction(INodeEditorApi editor) : base(editor)
    {
    }

    public override bool CanExecute() => true;

    public override void Execute()
    {
        var flowsCount = _editor.AllNodes.Values.Count(s => s is FlowNode);
        var flow = new FlowNode() { Name = "flow " + flowsCount };
        _editor.AddNodes([flow]);
        _addedFlowNodeId = flow.Id;
        _editor.ChangeFlow(flow);
    }

    public override void Undo()
    {
        var targetFlow = (FlowNode)_editor.AllNodes[_addedFlowNodeId!];
        DeleteFlowNodeAction.SwitchNextActuallyFlow(_editor, _addedFlowNodeId!);
        _editor.DeleteNodes([targetFlow]);
    }
}
