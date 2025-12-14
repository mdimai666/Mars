using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Core.Extensions;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpInNode/HttpInNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpInNode : Node
{
    public override string DisplayName => Name.AsNullIfEmpty() ?? (Method + " " + (UrlPattern.AsNullIfEmpty() ?? base.Label));

    public string Method { get; set; } = "GET";
    public string UrlPattern { get; set; } = "";

    public string[] MethodVariants = { "GET", "POST", "PUT", "DELETE", "HEAD" };

    public HttpInNode()
    {
        isInjectable = false;
        Color = "#e7e6af";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/web-48.png";
    }

}
