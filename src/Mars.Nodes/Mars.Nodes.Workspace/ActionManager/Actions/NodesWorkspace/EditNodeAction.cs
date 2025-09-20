using System.Text.Json;
using Mars.Nodes.Core;
using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;

public class EditNodeAction : BaseEditorHistoryAction
{
    private string _oldNodeValue;
    private string _newNodeValue;

    public EditNodeAction(INodeEditorApi editor, Node oldValue, Node newValue) : base(editor)
    {
        _oldNodeValue = ToJson(oldValue);
        _newNodeValue = ToJson(newValue);
    }

    public override bool CanExecute() => !_oldNodeValue.Equals(_newNodeValue);

    public override void Execute()
    {
        var newNode = FromJson(_newNodeValue);
        _editor.SaveNode(newNode, changed: true);

    }

    public override void Undo()
    {
        var oldNode = FromJson(_oldNodeValue);
        _editor.SaveNode(oldNode, changed: false);
    }

    string ToJson(Node node)
        => JsonSerializer.Serialize(node);

    Node FromJson(string json)
        => JsonSerializer.Deserialize<Node>(json)!;
}
