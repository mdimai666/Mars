using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;
using Mars.WebApp.Nodes.Models.NodeEntityQuery;
using Mars.WebApp.Nodes.Nodes;

namespace Mars.WebApp.Nodes.Examples;

internal class AppEntityDeleteNodeAfterReadNodeSimpleExample : INodeExample<AppEntityDeleteNode>
{
    public string Name => "Delete readed data";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle()
    {
        var forDeleteTag = "deleteTag";

        var expression = $"post.Where(post.Tags.Contains(\"{forDeleteTag}\")).ToList()";

        var entityReadQuery = new NodeEntityQueryRequestModel
        {
            EntityName = "post",
            CallChains = [
                new() { MethodName = "Where", Parameters = [$"post.Tags.Contains(\"{forDeleteTag}\")"] },
                new() { MethodName = "ToList", },
            ]
        };

        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new AppEntityReadNode { ExpressionInput = NodeExpressionInput.Builder, Expression = expression, Query = entityReadQuery })
            .AddNext(new AppEntityDeleteNode())
            .AddNext(new DebugNode())
            .Build();
    }
}
