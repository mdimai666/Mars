using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class HttpInNodeSimpleExample1 : INodeExample<HttpInNode>
{
    public string Name => "Simple HttpIn";
    public string Description => "A simple example of an HTTP In Node that responds to GET requests.";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new HttpInNode
            {
                Method = "GET",
                UrlPattern = "/example" + editorState.Nodes.Length
            })
            .AddNext(new TemplateNode
            {
                Template = "Template: {{Payload}}"
            })
            .AddNext(new HttpResponseNode(), new DebugNode { CompleteInputMessage = true })
            .Build();
    }
}
