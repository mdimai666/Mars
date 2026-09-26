using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Core.Extensions;

namespace Mars.Nodes.Core.Nodes.Network;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/docs/HttpInNode/HttpInNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpInNode : Node, INodeOutputValueSpec
{
    public override string TypeId => "core.HttpInNode";

    public override string DisplayName => Name.AsNullIfEmpty() ?? (Method + " " + (UrlPattern.AsNullIfEmpty() ?? base.Label));

    public string Method { get; set; } = "GET";
    public string UrlPattern { get; set; } = "";

    public string[] MethodVariants = { "GET", "POST", "PUT", "DELETE", "HEAD" };

    public bool IsRequireAuthorize { get; set; }
    public string[] AllowedRoles { get; set; } = [];

    [Display(Description = "Поддержка multipart-данных. Разрешить загрузку файлов.")]
    public bool AllowMultipart { get; set; }

    [Display(Description = "строковое представление размера")]
    public string MaxFileSize { get; set; } = "10mb";

    public HttpInNode()
    {
        Color = "#e7e6af";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/http-in.svg";
    }

    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        yield return new OutputValueSpec(nameof(NodeMsg.Payload), VarNode.ObjectTypeName,
            Description: "request body: string, JSON (JsonNode) or form-data — by request Content-Type");
    }
}
