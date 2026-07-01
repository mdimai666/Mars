using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class CallNodeHandlebarsTemplateExample : INodeExample<CallNode>
{
    public string Name => "Write template";
    public string Description => "CallNode called on render template";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new CallNode() { Name = "templateFunc1" })
            .AddNext(new TemplateNode() { Template = "<div>Template: {{Payload}}</div>" })
            .AddNext(new CallResponseNode())
            .Build();
    }
}
