using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/HttpRequestNode/HttpRequestNode{.lang}.md")]
public class HttpRequestNode : Node
{

    public string Method { get; set; } = "GET";
    public string Url { get; set; } = "http://localhost";

    public string[] MethodVariants = { "GET", "POST", "PUT", "DELETE", "HEAD" };


    public HttpRequestNode()
    {
        haveInput = true;
        Color = "#e7e6af";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/web2-48.png";
    }
}
