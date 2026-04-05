using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class FunctionNodePostListExample : INodeExample<FunctionNode>
{
    public string Name => "post list by service";
    public string Description => "List post using PostService";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new FunctionNode()
            {
                Code = """
                        using Mars.Host.Shared.Services;

                        var ps = RED.ServiceProvider.GetRequiredService<IPostService>();
                        var posts = await ps.List(new () { Type = "post", Skip = 0, Take = 2 }, default);

                        return posts;
                        """
            })
            .AddNext(new DebugNode())
            .Build();
    }
}
