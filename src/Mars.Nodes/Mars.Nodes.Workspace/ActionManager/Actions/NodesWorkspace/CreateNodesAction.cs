using Mars.Nodes.Core;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class CreateNodesAction : BaseEditorHistoryAction
{
    private readonly string _newNodesJson;
    private readonly bool _startDrag;

    public CreateNodesAction(INodeEditorApi editor, IReadOnlyCollection<Node> newNodes, bool startDrag = false) : base(editor)
    {
        _newNodesJson = NodesToJson(newNodes);
        _startDrag = startDrag;
    }

    public override bool CanExecute() => true;

    public override void Execute()
    {
        // Тут сложность в том что ноде задается место только после MouseUp, и он уже добавлен
        // поэтому добавляем их если только их уже нет
        var nodes = NodesFromJson(_newNodesJson);
        var nonExistNodes = nodes.Where(node => !_editor.AllNodes.ContainsKey(node.Id)).ToList();

        if (nonExistNodes.Any())
        {
            _editor.AddNodes(nonExistNodes);
            _editor.NodeWorkspace.RedrawWires();
        }

        if (_startDrag)
        {
            var createdNodes = nodes.Select(node => _editor.AllNodes[node.Id]);
            _editor.NodeWorkspace.StartDragNodes(createdNodes);
        }
    }

    public override void Undo()
    {
        var nodesCopy = NodesFromJson(_newNodesJson);
        var targetNodes = nodesCopy.Select(s => _editor.AllNodes[s.Id]).ToList();

        _editor.DeleteNodes(targetNodes);
        _editor.NodeWorkspace.RedrawWires();
    }
}
