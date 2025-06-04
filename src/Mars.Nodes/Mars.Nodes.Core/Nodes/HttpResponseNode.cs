using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpResponseNode/HttpResponseNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpResponseNode : Node
{
    public HttpResponseNode()
    {
        isInjectable = false;
        Color = "#e7e6af";
        this.HaveInput = true;
        //Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/web-48.png";
    }

}

