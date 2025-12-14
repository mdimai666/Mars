using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Utils;

namespace Mars.Nodes.Core.Examples.Nodes;

public class EndpointNodeSimpleExample1 : INodeExample<EndpointNode>
{
    public string Name => "EndpointNode example1";
    public string Description => "A smart EndpointNode of an HTTP that responds to POST requests.";

    public IReadOnlyCollection<Node> Handle()
    {
        return NodesWorkflowBuilder.Create()
            .AddNext(new EndpointNode
            {
                Method = "POST",
                UrlPattern = "/example1",
                IsRequireAuthorize = false,
                EndpointInputModel = EndpointInputModelType.JsonSchema,
            })
            .AddNext(new HttpResponseNode(), new DebugNode { CompleteInputMessage = true })
            .Build();
    }
}
