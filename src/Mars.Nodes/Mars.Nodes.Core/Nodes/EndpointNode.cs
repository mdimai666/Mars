using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Core.Extensions;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/EndpointNode/EndpointNode{.lang}.md")]
[Display(GroupName = "network")]
public class EndpointNode : Node
{
    public override string DisplayName => Name.AsNullIfEmpty() ?? (Method + " " + (UrlPattern.AsNullIfEmpty() ?? base.Label));
    public string Method { get; set; } = "POST";
    public string UrlPattern { get; set; } = "";

    public string[] MethodVariants = { "GET", "POST", "PUT", "DELETE" };

    public bool IsRequireAuthorize { get; set; }
    public IReadOnlyCollection<string> AllowedRoles { get; set; } = [];

    public EndpointInputModelType EndpointInputModel { get; set; }
    public string JsonSchema { get; set; } = JsonSchemaExample;

    public const string HelperJsonSchemaGetStartedUrl = "https://json-schema.org/understanding-json-schema/about";

    public const string JsonSchemaExample = """
        {
          "type": "object",
          "properties": {
            "first_name": { "type": "string" },
            "last_name": { "type": "string" },
            "birthday": { "type": "string", "format": "date" },
            "address": {
               "type": "object",
               "properties": {
                 "street_address": { "type": "string" },
                 "city": { "type": "string" },
                 "state": { "type": "string" },
                 "country": { "type" : "string" }
               }
            }
          },
          "required": [ "first_name", "last_name" ],
        }
        """;

    public EndpointNode()
    {
        Color = "#3c91de";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/azure-icon-service-Private-Endpoints.svg";
    }

}

public enum EndpointInputModelType
{
    String,
    JsonSchema,
}
