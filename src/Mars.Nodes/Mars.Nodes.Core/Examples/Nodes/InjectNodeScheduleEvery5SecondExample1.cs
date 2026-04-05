using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class InjectNodeScheduleEvery5SecondExample1 : INodeExample<InjectNode>
{
    public string Name => "Every 5 second";
    public string Description => "Execute every 5 second";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { IsSchedule = true, ScheduleCronMask = "0/5 * * * * ?" })
            .AddNext(new DebugNode())
            .Build();
    }
}
