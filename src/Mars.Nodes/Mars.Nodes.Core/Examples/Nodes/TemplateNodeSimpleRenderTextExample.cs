using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class TemplateNodeSimpleRenderTextExample : INodeExample<TemplateNode>
{
    public string Name => "Simple text render";
    public string Description => "Render input.Payload with text";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode() { Payload = "123", Name = "123" })
            .AddNext(new TemplateNode() { Template = "<div>Template: {{Payload}}</div>" })
            .AddNext(new DebugNode())
            .Build();
    }
}
