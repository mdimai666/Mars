using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class ForeachNodeIterateStringLettersExample1 : INodeExample<ForeachNode>
{
    public string Name => "Foreach iterate string letters";
    public string Description => "Foreach iterate string letters";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        var foreachNode = new ForeachNode() { Kind = EForeachKind.PayloadArray };
        var templateDebugNode = new DebugNode();
        var templateNode = new TemplateNode()
        {
            Template = "Value: {{Payload}}",
            Name = "Iterate",
            Wires = [[new NodeWire(templateDebugNode.Id), new NodeWire(foreachNode.Id, 1)]]
        };

        var builder = NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "1234", Name = "1234" })
            .AddNext(foreachNode)
            .AddNext(new TemplateNode() { Template = "Finish", Name = "Finish" })
            .AddNext(new DebugNode())
            .AddIndependent(templateNode, templateDebugNode);

        var fen1 = builder.BuilderItems[foreachNode.Id];
        fen1.Node.Wires.ElementAt(1).Add(templateNode.Id);

        var t1 = builder.BuilderItems[templateNode.Id];
        t1.Generation = fen1.Generation;
        t1.ElementRowIndex = 1;

        var d1 = builder.BuilderItems[templateDebugNode.Id];
        d1.Generation = t1.Generation + 1;
        d1.ElementRowIndex = 1;

        return builder.Build();
    }
}
