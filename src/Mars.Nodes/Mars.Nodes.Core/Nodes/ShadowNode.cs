using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/ShadowNode/ShadowNode{.lang}.md")]
public class ShadowNode
{
    public string CopyNodeId { get; set; } = "";
    public string CopyNodeType { get; set; } = "";

    public ShadowNode()
    {
        //Icon = "_content/NodeWorkspace/nodes/chunk-48.png";
    }
}
