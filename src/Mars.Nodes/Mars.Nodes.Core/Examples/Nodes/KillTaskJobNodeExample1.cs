using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class KillTaskJobNodeExample1 : INodeExample<KillTaskJobNode>
{
    public string Name => "KillTaskJobNode example";
    public string Description => "Kill task and start new";

    public IReadOnlyCollection<Node> Handle()
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(
                NodesWorkflowBuilder.Create().AddNext(new TemplateNode { Name = "First call", Template = "First call" })
                                                .AddNext(new DebugNode()),
                NodesWorkflowBuilder.Create().AddNext(new DelayNode { DelayMillis = 1000 })
                                                .AddNext(new TemplateNode { Name = "Not be called", Template = "not" })
                                                .AddNext(new DebugNode()),
                NodesWorkflowBuilder.Create().AddNext(new DelayNode { DelayMillis = 500 })
                                                .AddNext(new KillTaskJobNode())
                                                .AddNext(new TemplateNode { Name = "new task", Template = "new task" })
                                                .AddNext(new DebugNode())
            )
            .Build();
    }
}
