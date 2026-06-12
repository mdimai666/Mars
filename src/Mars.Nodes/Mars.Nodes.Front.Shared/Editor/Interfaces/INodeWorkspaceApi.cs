using Mars.Nodes.Core;
using Mars.Shared.Utils;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Front.Shared.Editor.Interfaces;

public interface INodeWorkspaceApi
{
    IReadOnlyDictionary<string, Node> FlowNodes { get; }
    IEnumerable<Wire> Wires { get; }
    ScrollInfo ScrollInfo { get; }
    MouseEventArgs LastMouseWorkspaceState { get; }

    event Action<IEnumerable<string>> OnDragNodesStarted;
    event Action<IEnumerable<string>> OnDragNodesEnded;

    IReadOnlyCollection<Node> SelectedNodes();
    IReadOnlyCollection<Wire> SelectedWires();

    void StartDragNodes(IEnumerable<Node> nodes, bool startMoveUnderCursor = true, float offsetX = 0, float offsetY = 0);
}
