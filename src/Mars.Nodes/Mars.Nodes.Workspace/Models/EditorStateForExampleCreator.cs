using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Workspace.ActionManager.CopyBuffer;

namespace Mars.Nodes.Workspace.Models;

internal class EditorStateForExampleCreator : IEditorState
{
    private readonly Lazy<Node[]> _lazyNodes;
    private readonly NodeEditor1 _nodeEditor;

    public Node[] Nodes => _lazyNodes.Value;

    public EditorStateForExampleCreator(NodeEditor1 nodeEditor)
    {
        _nodeEditor = nodeEditor;
        _lazyNodes = new Lazy<Node[]>(() =>
        {
            var json = NodesCopyBufferItem.NodesToJson(nodeEditor.AllNodes.Values, nodeEditor.NodesJsonSerializerOptions);
            var nodesCopy = NodesCopyBufferItem.NodesFromJson(json, nodeEditor.NodesJsonSerializerOptions);
            return nodesCopy;
        });
    }

    public InlineFunctionNode? CreateInlineFunctionNodeById(string nodeTypeId)
    {
        return _nodeEditor.CreateInlineFunctionNodeById(nodeTypeId);
    }
}
