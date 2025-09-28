using System.Drawing;
using Mars.Nodes.Core;

namespace Mars.Nodes.EditorApi.Interfaces;

public interface INodeWorkspaceApi
{
    IReadOnlyDictionary<string, Node> FlowNodes { get; }
    IEnumerable<Wire> Wires { get; }
    IReadOnlyCollection<Node> SelectedNodes();
    IReadOnlyCollection<Wire> SelectedWires();

    void RedrawWires();
    void CallStateHasChanged();
    void StartDragNodes(IEnumerable<Node> nodes, bool startMoveUnderCursor = true);
}
