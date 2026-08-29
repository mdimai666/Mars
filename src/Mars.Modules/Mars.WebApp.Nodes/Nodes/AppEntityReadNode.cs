using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Nodes.Core;
using Mars.WebApp.Nodes.Models.NodeEntityQuery;

namespace Mars.WebApp.Nodes.Nodes;

[FunctionApiDocument("./_content/Mars.WebApp.Nodes.Front/docs/AppEntityReadNode/AppEntityReadNode{.lang}.md")]
[Display(GroupName = "entity")]
public class AppEntityReadNode : Node
{
    /// <summary>
    /// Example: Posts.Where(post.Title!=\"111\").ToList()
    /// </summary>
    public string Expression { get; set; } = "post.Where(post.Title!=\"111\").ToList()";
    public NodeEntityQueryRequestModel Query { get; set; } = new();
    public NodeExpressionInput ExpressionInput { get; set; } = NodeExpressionInput.Builder;

    public AppEntityReadNode()
    {
        Color = "#21a366";
        Icon = "_content/Mars.Nodes.Workspace/nodes/db-48.png";
        Inputs = [new()];
        Outputs = [
            new (){ Label = "Success" },
        ];
    }
}

public enum NodeExpressionInput
{
    Builder,
    String,
}
