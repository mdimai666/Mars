using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class QueueNodeQueueWithTimerExample : INodeExample<QueueNode>
{
    public string Name => "Queue with timer";
    public string Description => "";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode())
            .AddNext(editorState.CreateInlineFunctionNodeById("core.InlineFunctionNode.Utils.GenerateSequentialArray")!)
            .AddNext(new SplitNode())
            .AddNext(new QueueNode() { MaxTask = 3 })
            .AddNext(NodesWorkflowBuilder.Create()
                        .AddNext(new TemplateNode { Name = "FINISH", Template = "FINISH" })
                        .AddNext(new DebugNode()),
                    NodesWorkflowBuilder.Create()
                        .AddNext(new TemplateNode { Name = "Item", Template = "item: {{Payload}}" })
                        .AddNext(new DebugNode(), new DelayNode() { DelayMillis = 2000 }))
            ;

        var delayNode = builder.Nodes.First(s => s is DelayNode);
        var queueNode = builder.Nodes.First(s => s is QueueNode);
        var templateIterateNode = builder.Nodes.First(s => s.Name == "Item");

        delayNode.Wires = [[new NodeWire(queueNode.Id, PortIndex: 1)]];
        queueNode.Wires = [[queueNode.Wires[0][0]], [templateIterateNode.Id]];

        var nodes = builder.Build();
        delayNode.X = queueNode.X;

        return nodes;
    }
}
