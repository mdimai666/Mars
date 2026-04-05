using Mars.Nodes.Core;
using Mars.Nodes.Workspace.ActionManager.CopyBuffer;

namespace Mars.Nodes.Workspace.Models;

internal class EditorStateForExampleCreator : IEditorState
{
    private readonly Lazy<Node[]> _lazyNodes;

    public Node[] Nodes => _lazyNodes.Value;

    public EditorStateForExampleCreator(NodeEditor1 nodeEditor)
    {
        _lazyNodes = new Lazy<Node[]>(() =>
        {
            var json = NodesCopyBufferItem.NodesToJson(nodeEditor.AllNodes.Values, nodeEditor.NodesJsonSerializerOptions);
            var nodesCopy = NodesCopyBufferItem.NodesFromJson(json, nodeEditor.NodesJsonSerializerOptions);
            return nodesCopy;
        });
    }
}
