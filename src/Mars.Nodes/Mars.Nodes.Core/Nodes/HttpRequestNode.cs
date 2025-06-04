using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpRequestNode/HttpRequestNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpRequestNode : Node
{

    public string Method { get; set; } = "GET";
    public string Url { get; set; } = "http://localhost";

    public string[] MethodVariants = { "GET", "POST", "PUT", "DELETE", "HEAD" };

    public HeaderItem[] Headers { get; set; } = [];

    public HttpRequestNode()
    {
        HaveInput = true;
        Color = "#e7e6af";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/web2-48.png";
    }
}

public class HeaderItem
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
