using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/HttpInNode/HttpInNode{.lang}.md")]
public class HttpInNode : Node
{

    public string Method { get; set; } = "GET";
    public string UrlPattern { get; set; } = "";

    public string[] MethodVariants = { "GET", "POST", "PUT", "DELETE", "HEAD" };


    public HttpInNode()
    {
        isInjectable = false;
        Color = "#e7e6af";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/web-48.png";
    }

}
