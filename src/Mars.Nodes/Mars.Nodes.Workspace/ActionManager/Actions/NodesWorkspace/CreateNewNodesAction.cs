using Mars.Nodes.Core;
using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class CreateNewNodesAction : BaseEditorHistoryAction
{
    private readonly string _newNodesJson;

    public CreateNewNodesAction(INodeEditorApi editor, IReadOnlyCollection<Node> newNodes) : base(editor)
    {
        _newNodesJson = NodesToJson(newNodes);

    }

    public override bool CanExecute() => true;

    public override void Execute()
    {
        // Тут сложность в том что ноде задается место только после MouseUp, и он уже добавлен
        // поэтому добавляем их если только их уже нет
        var nodes = NodesFromJson(_newNodesJson);
        var toAddNodes = new List<Node>();
        foreach (var node in nodes)
        {
            if (!_editor.AllNodes.ContainsKey(node.Id))
                toAddNodes.Add(node);
        }

        if (toAddNodes.Any())
        {
            _editor.AddNodes(toAddNodes);
            _editor.NodeWorkspace.RedrawWires();
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
