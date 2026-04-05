using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class DelayNodeWaitSecondExample : INodeExample<DelayNode>
{
    public string Name => "Wait second";
    public string Description => "Wait second";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(new DelayNode() { DelayMillis = 1000 })
            .AddNext(new DebugNode())
            .Build();
    }
}
