using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class InjectNodeSimpleExample1 : INodeExample<InjectNode>
{
    public string Name => "Inject->Debug";
    public string Description => "Using Inject call.";

    public IReadOnlyCollection<Node> Handle()
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new DebugNode())
            .Build();
    }
}
