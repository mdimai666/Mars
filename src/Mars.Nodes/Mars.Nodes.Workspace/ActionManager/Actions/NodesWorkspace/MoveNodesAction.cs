using Mars.Nodes.Core.Utils;
using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class MoveNodesAction : BaseEditorHistoryAction
{
    /// <summary>
    ///  NodeId, Point
    /// </summary>
    readonly IReadOnlyDictionary<string, MovePoints> _moves;

    public MoveNodesAction(INodeEditorApi editor, IReadOnlyDictionary<string, MovePoints> movePoints) : base(editor)
    {
        _moves = movePoints;
    }

    public override bool CanExecute() => true;

    public override void Execute()
    {
        foreach (var (nodeId, p) in _moves)
        {
            var node = _editor.AllNodes[nodeId];
            node.X = p.End.X;
            node.Y = p.End.Y;
        }
        _editor.NodeWorkspace.RedrawWires();
    }

    public override void Undo()
    {
        foreach (var (nodeId, p) in _moves)
        {
            var node = _editor.AllNodes[nodeId];
            node.X = p.Start.X;
            node.Y = p.Start.Y;
        }
        _editor.NodeWorkspace.RedrawWires();
    }
}
