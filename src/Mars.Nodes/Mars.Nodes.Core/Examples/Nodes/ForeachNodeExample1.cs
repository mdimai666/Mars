using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class ForeachNodeExample1 : INodeExample<ForeachNode>
{
    public string Name => "Foreach example 1";
    public string Description => "Foreach example 1";

    public IReadOnlyCollection<Node> Handle()
    {
        var foreachNode = new ForeachNode() { Kind = EForeachKind.PayloadArray };
        var templateDebugNode = new DebugNode();
        var templateNode = new TemplateNode()
        {
            Template = "Value: {{Payload}}",
            Wires = [[new NodeWire(templateDebugNode.Id), new NodeWire(foreachNode.Id, 1)]]
        };

        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "1234" })
            .AddNext(foreachNode)
            .AddNext(new TemplateNode() { Template = "Finish" })
            .AddNext(new DebugNode())
            .AddIndependent(templateNode, templateDebugNode);

        var fen1 = builder.BuilderItem[foreachNode.Id];
        fen1.Node.Wires.ElementAt(1).Add(templateNode.Id);

        var t1 = builder.BuilderItem[templateNode.Id];
        t1.Generation = fen1.Generation;
        t1.ElementIndex = 1;

        var d1 = builder.BuilderItem[templateDebugNode.Id];
        d1.Generation = t1.Generation + 1;
        d1.ElementIndex = 1;

        return builder.Build();
    }
}
