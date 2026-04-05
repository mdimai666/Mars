using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class FunctionNodeGetRandomNumberExample : INodeExample<FunctionNode>
{
    public string Name => "Get random number";
    public string Description => "FunctionNode Get random number";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new FunctionNode()
            {
                Code = """
                        var x = Random.Shared.Next(1, 100);
                        return $"random number is {x}";
                        """
            })
            .AddNext(new DebugNode())
            .Build();
    }
}
