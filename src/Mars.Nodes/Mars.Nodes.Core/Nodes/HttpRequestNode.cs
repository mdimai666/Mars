using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Core.Extensions;
using Mars.Nodes.Core.Fields;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpRequestNode/HttpRequestNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpRequestNode : Node
{
    public override string DisplayName => Name.AsNullIfEmpty() ?? Url.AsNullIfEmpty() ?? base.Label;
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = "http://localhost";

    public string[] MethodVariants = { "GET", "POST", "PUT", "DELETE", "HEAD" };

    public HeaderItem[] Headers { get; set; } = [];

    public InputConfig<AuthFlowConfigNode> AuthConfig { get; set; }

    public HttpRequestNode()
    {
        Inputs = [new()];
        Color = "#e7e6af";
        Outputs = [new NodeOutput()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/web2-48.png";
    }

    public static IReadOnlyCollection<string> ExampleHeaders =
    [
        "Content-Type=application/json",
        "Content-Type=application/x-www-form-urlencoded",
        "Content-Type=text/plain",
        "Content-Type=application/xml",
        "Content-Type=text/html",

        "Authorization=Bearer <token>",

        "Accept=application/json",
        "Accept=application/xml",
        "Accept=text/html",

        // Common browser headers
        "User-Agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
        "Cache-Control=no-cache",
        "Pragma=no-cache",
        "Connection=keep-alive",
        "Accept-Encoding=gzip, deflate, br",
        "Accept-Language=en-US,en;q=0.9",

        // Custom API example
        "X-Api-Key=<your-api-key>",
    ];
}

public record HeaderItem
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
