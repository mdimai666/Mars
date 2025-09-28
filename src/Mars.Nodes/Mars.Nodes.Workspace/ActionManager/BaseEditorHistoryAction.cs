using System.Text.Json;
using Mars.Nodes.Core;
using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager;

public abstract class BaseEditorHistoryAction : IEditorHistoryAction
{
    public Guid Guid { get; } = Guid.NewGuid();

    protected readonly INodeEditorApi _editor;

    protected BaseEditorHistoryAction(INodeEditorApi editor)
    {
        _editor = editor;
    }

    public string NodesToJson(IEnumerable<Node> nodes)
        => JsonSerializer.Serialize(nodes);

    public Node[] NodesFromJson(string json)
        => JsonSerializer.Deserialize<Node[]>(json)!;

    public abstract void Execute();
    public abstract void Undo();
    public abstract bool CanExecute();
}
