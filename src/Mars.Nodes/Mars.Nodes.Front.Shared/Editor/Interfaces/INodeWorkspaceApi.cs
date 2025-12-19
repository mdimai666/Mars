using System.Drawing;
using Mars.Nodes.Core;

namespace Mars.Nodes.Front.Shared.Editor.Interfaces;

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
