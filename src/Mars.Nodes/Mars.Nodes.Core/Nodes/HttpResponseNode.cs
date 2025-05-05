using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/HttpResponseNode/HttpResponseNode{.lang}.md")]
public class HttpResponseNode : Node
{
    public HttpResponseNode()
    {
        isInjectable = false;
        Color = "#e7e6af";
        this.haveInput = true;
        //Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/web-48.png";
    }

}

