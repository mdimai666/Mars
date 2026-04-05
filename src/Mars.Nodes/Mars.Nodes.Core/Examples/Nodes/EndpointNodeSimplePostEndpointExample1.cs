using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class EndpointNodeSimplePostEndpointExample1 : INodeExample<EndpointNode>
{
    public string Name => "POST endpoint";
    public string Description => "A smart EndpointNode of an HTTP that responds to POST requests.";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState)
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new EndpointNode
            {
                Method = "POST",
                UrlPattern = "/example" + editorState.Nodes.Length,
                IsRequireAuthorize = false,
                EndpointInputModel = EndpointInputModelType.JsonSchema,
            })
            .AddNext(new HttpResponseNode(), new DebugNode { CompleteInputMessage = true })
            .Build();
    }
}
